#include "pch.h"
#include "Engine.h"
#include <d3d11.h>
#include <d3d9.h>

ID3D11Device* g_pd3d11Device = nullptr;
ID3D11DeviceContext* g_pd3d11Context = nullptr;
ID3D11Texture2D* g_pd3d11Texture = nullptr;

IDirect3D9Ex* g_pD3D9 = nullptr;
IDirect3DDevice9Ex* g_pD3D9Device = nullptr;
IDirect3DTexture9* g_pD3D9Texture = nullptr;
IDirect3DSurface9* g_pD3D9Surface = nullptr;

HANDLE g_hSharedTexture = nullptr;

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

	hr = g_pD3D9->CreateDeviceEx(
		D3DADAPTER_DEFAULT,
		D3DDEVTYPE_HAL,
		d3dpp.hDeviceWindow,
		D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
		&d3dpp,
		NULL,
		&g_pD3D9Device
	);

	return SUCCEEDED(hr);
}

BLURBERRY_API bool Initialize(int width, int height)
{
	HRESULT hr = S_OK;

	if (!InitDX9(NULL)) return false;

	UINT createDeviceFlags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#ifdef _DEBUG
	createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

	D3D_FEATURE_LEVEL featureLevels[] = { D3D_FEATURE_LEVEL_10_1, D3D_FEATURE_LEVEL_10_0 };
	D3D_FEATURE_LEVEL featureLevel;

	hr = D3D11CreateDevice(
		nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, createDeviceFlags,
		featureLevels, 2, D3D11_SDK_VERSION,
		&g_pd3d11Device, &featureLevel, &g_pd3d11Context
	);
	if (FAILED(hr)) return false;

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

	IDXGIResource* pDXGIResource = nullptr;
	hr = g_pd3d11Texture->QueryInterface(__uuidof(IDXGIResource), (void**)&pDXGIResource);
	if (SUCCEEDED(hr)) {
		pDXGIResource->GetSharedHandle(&g_hSharedTexture);
		pDXGIResource->Release();
	}
	else {
		return false;
	}

	if (g_hSharedTexture) {
		hr = g_pD3D9Device->CreateTexture(
			width, height, 1,
			D3DUSAGE_RENDERTARGET,
			D3DFMT_A8R8G8B8,
			D3DPOOL_DEFAULT,
			&g_pD3D9Texture,
			&g_hSharedTexture
		);

		if (FAILED(hr)) return false;

		g_pD3D9Texture->GetSurfaceLevel(0, &g_pD3D9Surface);
	}

	return true;
}

BLURBERRY_API void Cleanup()
{
	if (g_pD3D9Surface) g_pD3D9Surface->Release();
	if (g_pD3D9Texture) g_pD3D9Texture->Release();
	if (g_hSharedTexture) g_hSharedTexture = nullptr;

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

BLURBERRY_API void UpdateFrame(unsigned char* pData, int rowPitch)
{
	if (!g_pd3d11Context || !g_pd3d11Texture) return;

	g_pd3d11Context->UpdateSubresource(g_pd3d11Texture, 0, nullptr, pData, rowPitch, 0);
}