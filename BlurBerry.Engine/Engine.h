#pragma once

#include <Windows.h>
#include <d3d11.h>

#ifdef BLURBERRY_EXPORTS
#define BLURBERRY_API extern "C" __declspec(dllexport)
#else
#define BLURBERRY_API extern "C" __declspec(dllimport)
#endif

BLURBERRY_API bool Initialize(int width, int height);

BLURBERRY_API bool OpenVideo(const char* filepath);

BLURBERRY_API bool GrabNextFrame();

BLURBERRY_API void Cleanup();

BLURBERRY_API HANDLE GetSharedHandle();