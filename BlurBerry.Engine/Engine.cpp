#include "pch.h"
#include "Engine.h"
#include <d3d11.h>

ID3D11Device* g_pd3dDevice = nullptr;
ID3D11DeviceContext* g_pImmediateContext = nullptr;
ID3D11Texture2D* g_pSharedTexture = nullptr;
HANDLE g_hSharedTexture = nullptr;

BLURBERRY_API bool Initialize(int width, int height)
{
	HRESULT hr = S_OK;

	UINT createDeviceFlags = 0;
#ifdef _DEBUG
	createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

	D3D_FEATURE_LEVEL featureLevels[] = { D3D_FEATURE_LEVEL_11_0 };
	D3D_FEATURE_LEVEL featureLevel;

	hr = D3D11CreateDevice(
		nullptr,
		D3D_DRIVER_TYPE_HARDWARE,
		nullptr,
		createDeviceFlags,
		featureLevels, 1,
		D3D11_SDK_VERSION,
		&g_pd3dDevice,
		&featureLevel,
		&g_pImmediateContext
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
	desc.CPUAccessFlags = 0;
	desc.MiscFlags = D3D11_RESOURCE_MISC_SHARED;

	hr = g_pd3dDevice->CreateTexture2D(&desc, nullptr, &g_pSharedTexture);
	if (FAILED(hr)) return false;

	IDXGIResource* pDXGIResource = nullptr;
	hr = g_pSharedTexture->QueryInterface(__uuidof(IDXGIResource), (void**)&pDXGIResource);
	if (SUCCEEDED(hr)) {
		pDXGIResource->GetSharedHandle(&g_hSharedTexture);
		pDXGIResource->Release();
	}

	return true;
}

BLURBERRY_API void Cleanup()
{
	if (g_pSharedTexture) g_pSharedTexture->Release();
	if (g_pImmediateContext) g_pImmediateContext->Release();
	if (g_pd3dDevice) g_pd3dDevice->Release();

	g_pSharedTexture = nullptr;
	g_pImmediateContext = nullptr;
	g_pd3dDevice = nullptr;
	g_hSharedTexture = nullptr;
}

BLURBERRY_API HANDLE GetSharedHandle()
{
	return g_hSharedTexture;
}

BLURBERRY_API void UpdateFrame(unsigned char* pData, int rowPitch)
{
	if (!g_pImmediateContext || !g_pSharedTexture) return;

	g_pImmediateContext->UpdateSubresource(
		g_pSharedTexture,
		0,
		nullptr,
		pData,
		rowPitch,
		0
	);
}