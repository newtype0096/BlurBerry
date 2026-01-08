#include "pch.h"
#include "Engine.h"
#include <d3d11.h>
#include <d3d9.h>
#include <vector>

// FFmpeg 헤더
extern "C" {
#include <libavformat/avformat.h>
#include <libavcodec/avcodec.h>
#include <libswscale/swscale.h>
#include <libavutil/imgutils.h>
}

// --------------------------------------------------------
// 전역 변수 (DirectX)
// --------------------------------------------------------
ID3D11Device* g_pd3d11Device = nullptr;
ID3D11DeviceContext* g_pd3d11Context = nullptr;
ID3D11Texture2D* g_pd3d11Texture = nullptr;

IDirect3D9Ex* g_pD3D9 = nullptr;
IDirect3DDevice9Ex* g_pD3D9Device = nullptr;
IDirect3DTexture9* g_pD3D9Texture = nullptr;
IDirect3DSurface9* g_pD3D9Surface = nullptr;
HANDLE g_hSharedTexture = nullptr;

// --------------------------------------------------------
// 전역 변수 (FFmpeg)
// --------------------------------------------------------
AVFormatContext* g_pFormatCtx = nullptr;
AVCodecContext* g_pCodecCtx = nullptr;
AVFrame* g_pFrame = nullptr;
AVPacket* g_pPacket = nullptr;
SwsContext* g_pSwsCtx = nullptr;
int g_videoStreamIdx = -1;

// 디코딩된 BGRA 데이터를 담을 임시 버퍼
std::vector<uint8_t> g_pixelBuffer;
int g_width = 0;
int g_height = 0;

// --------------------------------------------------------
// 내부 함수
// --------------------------------------------------------
bool InitDX9(HWND hwnd)
{
	HRESULT hr = Direct3DCreate9Ex(D3D_SDK_VERSION, &g_pD3D9);
	if (FAILED(hr)) return false;

	D3DPRESENT_PARAMETERS d3dpp;
	ZeroMemory(&d3dpp, sizeof(d3dpp));
	d3dpp.Windowed = TRUE;
	d3dpp.SwapEffect = D3DSWAPEFFECT_DISCARD;
	d3dpp.hDeviceWindow = GetShellWindow();
	d3dpp.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;

	hr = g_pD3D9->CreateDeviceEx(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, d3dpp.hDeviceWindow,
		D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
		&d3dpp, NULL, &g_pD3D9Device);

	return SUCCEEDED(hr);
}

// --------------------------------------------------------
// API 구현
// --------------------------------------------------------

BLURBERRY_API bool Initialize(int width, int height)
{
    g_width = width;
    g_height = height;
    HRESULT hr = S_OK;

    // 1. DX9 Init
    if (!InitDX9(NULL)) return false;

    // 2. DX11 Device
    UINT createDeviceFlags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#ifdef _DEBUG
    createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif
    D3D_FEATURE_LEVEL featureLevels[] = { D3D_FEATURE_LEVEL_10_1, D3D_FEATURE_LEVEL_10_0 };
    D3D_FEATURE_LEVEL featureLevel;

    hr = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, createDeviceFlags,
        featureLevels, 2, D3D11_SDK_VERSION, &g_pd3d11Device, &featureLevel, &g_pd3d11Context);
    if (FAILED(hr)) return false;

    // 3. Texture (Shared)
    D3D11_TEXTURE2D_DESC desc;
    ZeroMemory(&desc, sizeof(desc));
    desc.Width = width;
    desc.Height = height;
    desc.MipLevels = 1;
    desc.ArraySize = 1;
    desc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    desc.SampleDesc.Count = 1;
    desc.Usage = D3D11_USAGE_DEFAULT;
    desc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
    desc.MiscFlags = D3D11_RESOURCE_MISC_SHARED;

    hr = g_pd3d11Device->CreateTexture2D(&desc, nullptr, &g_pd3d11Texture);
    if (FAILED(hr)) return false;

    // 4. Share Handle
    IDXGIResource* pDXGIResource = nullptr;
    hr = g_pd3d11Texture->QueryInterface(__uuidof(IDXGIResource), (void**)&pDXGIResource);
    if (SUCCEEDED(hr)) {
        pDXGIResource->GetSharedHandle(&g_hSharedTexture);
        pDXGIResource->Release();
    }
    else return false;

    // 5. DX9 Link
    if (g_hSharedTexture) {
        hr = g_pD3D9Device->CreateTexture(width, height, 1, D3DUSAGE_RENDERTARGET,
            D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT, &g_pD3D9Texture, &g_hSharedTexture);
        if (FAILED(hr)) return false;
        g_pD3D9Texture->GetSurfaceLevel(0, &g_pD3D9Surface);
    }

    // ★ 하얀 화면 방지: 검은색으로 초기화
    // (픽셀 버퍼를 0으로 채워서 한번 업로드)
    std::vector<uint8_t> blackBuffer(width * height * 4, 0); // All Zero = Black Transparent
    // Alpha를 255(Opaque)로 채우고 싶으면 반복문 돌려야 함. 일단 0으로 둠.
    g_pd3d11Context->UpdateSubresource(g_pd3d11Texture, 0, nullptr, blackBuffer.data(), width * 4, 0);

    return true;
}

BLURBERRY_API bool OpenVideo(const char* filepath)
{
    // FFmpeg 초기화 로직 (Step 1과 유사)
    if (avformat_open_input(&g_pFormatCtx, filepath, nullptr, nullptr) != 0) return false;
    if (avformat_find_stream_info(g_pFormatCtx, nullptr) < 0) return false;

    // 코덱 찾기
    g_videoStreamIdx = -1;
    for (unsigned int i = 0; i < g_pFormatCtx->nb_streams; i++) {
        if (g_pFormatCtx->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO) {
            g_videoStreamIdx = i;
            break;
        }
    }
    if (g_videoStreamIdx == -1) return false;

    AVCodecParameters* pCodecParams = g_pFormatCtx->streams[g_videoStreamIdx]->codecpar;
    const AVCodec* pCodec = avcodec_find_decoder(pCodecParams->codec_id);
    g_pCodecCtx = avcodec_alloc_context3(pCodec);
    avcodec_parameters_to_context(g_pCodecCtx, pCodecParams);

    // 멀티스레드 디코딩 (성능 향상 필수)
    // g_pCodecCtx->thread_count = 4; 

    if (avcodec_open2(g_pCodecCtx, pCodec, nullptr) < 0) return false;

    // 프레임 할당
    g_pFrame = av_frame_alloc();
    g_pPacket = av_packet_alloc();

    // 색상 변환 컨텍스트 (YUV -> BGRA)
    // 주의: Initialize에서 받은 해상도(g_width)로 맞춤 (Scale 기능 포함)
    g_pSwsCtx = sws_getContext(
        g_pCodecCtx->width, g_pCodecCtx->height, g_pCodecCtx->pix_fmt,
        g_width, g_height, AV_PIX_FMT_BGRA, // Target Format
        SWS_BILINEAR, nullptr, nullptr, nullptr
    );

    // 픽셀 버퍼 할당
    g_pixelBuffer.resize(g_width * g_height * 4);

    return true;
}

BLURBERRY_API bool GrabNextFrame()
{
    if (!g_pFormatCtx || !g_pCodecCtx) return false;

    while (av_read_frame(g_pFormatCtx, g_pPacket) >= 0) {
        if (g_pPacket->stream_index == g_videoStreamIdx) {
            if (avcodec_send_packet(g_pCodecCtx, g_pPacket) == 0) {
                if (avcodec_receive_frame(g_pCodecCtx, g_pFrame) == 0) {

                    // 1. 디코딩 성공 -> BGRA 변환
                    uint8_t* dest[4] = { g_pixelBuffer.data(), nullptr, nullptr, nullptr };
                    int dest_linesize[4] = { g_width * 4, 0, 0, 0 };

                    sws_scale(g_pSwsCtx, g_pFrame->data, g_pFrame->linesize,
                        0, g_pCodecCtx->height, dest, dest_linesize);

                    // 2. GPU 텍스처 업데이트
                    g_pd3d11Context->UpdateSubresource(
                        g_pd3d11Texture, 0, nullptr,
                        g_pixelBuffer.data(), g_width * 4, 0
                    );

                    g_pd3d11Context->Flush();

                    // 패킷 해제하고 리턴 true
                    av_packet_unref(g_pPacket);
                    return true;
                }
            }
        }
        av_packet_unref(g_pPacket);
    }
    return false; // EOF or Error
}

BLURBERRY_API void Cleanup()
{
    // FFmpeg 정리
    if (g_pFrame) av_frame_free(&g_pFrame);
    if (g_pPacket) av_packet_free(&g_pPacket);
    if (g_pCodecCtx) avcodec_free_context(&g_pCodecCtx);
    if (g_pFormatCtx) avformat_close_input(&g_pFormatCtx);
    if (g_pSwsCtx) sws_freeContext(g_pSwsCtx);

    // DX 정리
    if (g_pD3D9Surface) g_pD3D9Surface->Release();
    if (g_pD3D9Texture) g_pD3D9Texture->Release();
    // g_hSharedTexture는 Release 안 함
    if (g_pd3d11Texture) g_pd3d11Texture->Release();
    if (g_pd3d11Context) g_pd3d11Context->Release();
    if (g_pd3d11Device) g_pd3d11Device->Release();
    if (g_pD3D9Device) g_pD3D9Device->Release();
    if (g_pD3D9) g_pD3D9->Release();
}

BLURBERRY_API HANDLE GetSharedHandle()
{
    return (HANDLE)g_pD3D9Surface;
}