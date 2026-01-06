// BlurBerry.Engine.Test.cpp : 이 파일에는 'main' 함수가 포함됩니다. 거기서 프로그램 실행이 시작되고 종료됩니다.
//

#include <iostream>
#include <fstream>
#include <vector>

#pragma warning(disable : 4819)
extern "C" {
#include <libavformat/avformat.h>
#include <libavcodec/avcodec.h>
#include <libswscale/swscale.h>
#include <libavutil/imgutils.h>
}
#pragma warning(default : 4819)

void SavePPM(unsigned char* pPixels, int width, int height, int iFrame) {
	char filename[32];
	sprintf_s(filename, "frame%d.ppm", iFrame);
	std::ofstream file(filename, std::ios::binary);

	// PPM 헤더
	// P6: 바이너리 RGB 포맷
	// width height: 이미지 크기
	// 255: 색상 한 개(R, G, B 각각)의 최대값
	file << "P6\n" << width << " " << height << "\n255\n";

	for (int y = 0; y < height; y++) {
		const unsigned char* row = pPixels + y * width * 4;
		for (int x = 0; x < width; x++) {
			const unsigned char* px = row + x * 4;

			// px에는 BGRA 순서로 데이터가 들어있음.
			// PPM 포맷은 RGB 순서로 데이터를 저장해야 함.
			// px[3]: Alpha (사용 안 함)
			// px[2]: Red
			// px[1]: Green
			// px[0]: Blue
			file.put(px[2]);
			file.put(px[1]);
			file.put(px[0]);
		}
	}
	file.close();
	std::cout << "Saved " << filename << std::endl;
}

int main()
{
	const char* filepath = "C:\\Users\\M\\Desktop\\hevc_4k25P_main_2.mp4";

	AVFormatContext* pFormatCtx = nullptr;
	if (avformat_open_input(&pFormatCtx, filepath, nullptr, nullptr) != 0) {
		std::cerr << "Could not open file." << std::endl;
		return -1;
	}

	if (avformat_find_stream_info(pFormatCtx, nullptr) < 0) {
		std::cerr << "Could not find stream info." << std::endl;
		return -1;
	}

	int videoStreamIdx = -1;
	const AVCodec* pCodec = nullptr;
	AVCodecParameters* pCodecParams = nullptr;

	for (unsigned int i = 0; i < pFormatCtx->nb_streams; i++) {
		if (pFormatCtx->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO) {
			videoStreamIdx = i;
			pCodecParams = pFormatCtx->streams[i]->codecpar;
			pCodec = avcodec_find_decoder(pCodecParams->codec_id);
			break;
		}
	}

	if (videoStreamIdx == -1) {
		std::cerr << "No video stream found." << std::endl;
		return -1;
	}

	AVCodecContext* pCodecCtx = avcodec_alloc_context3(pCodec);
	avcodec_parameters_to_context(pCodecCtx, pCodecParams);

	if (avcodec_open2(pCodecCtx, pCodec, nullptr) < 0) {
		std::cerr << "Could not open codec." << std::endl;
		return -1;
	}

	std::cout << "Video Info: " << pCodecCtx->width << "x" << pCodecCtx->height << std::endl;

	AVFrame* pFrame = av_frame_alloc();
	AVPacket* pPacket = av_packet_alloc();

	struct SwsContext* sws_ctx = sws_getContext(
		pCodecCtx->width, pCodecCtx->height, pCodecCtx->pix_fmt,
		pCodecCtx->width, pCodecCtx->height, AV_PIX_FMT_BGRA,
		SWS_BILINEAR, nullptr, nullptr, nullptr
	);

	// 4는 BGRA라서 4.
	// B(Blue): 1 byte
	// G(Green): 1 byte
	// R(Red): 1 byte
	// A(Alpha): 1 byte
	std::vector<uint8_t> buffer(pCodecCtx->width * pCodecCtx->height * 4);

	// 배열 크기가 4개인 이유
	// FFmpeg은 Packed 포맷과 Planar 포맷을 모두 지원하기 위해 AVFrame->data 배열을 무조건 4칸으로 고정해둠.
	//	Planar 포맷인 경우 (예: YUV420P)
	//		data[0]: Y(밝기)
	//		data[1]: U(색상1)
	//		data[2]: V(색상2)
	//		data[3]: 사용 안 함
	//	Packed 포맷인 경우 (예: BGRA)
	//		data[0]: BGRA 전체
	//		data[1]: 사용 안 함
	//		data[2]: 사용 안 함
	//		data[3]: 사용 안 함
	uint8_t* dest[4] = { buffer.data(), nullptr, nullptr, nullptr };

	// 4는 BGRA라서 4.
	int dest_linesize[4] = { pCodecCtx->width * 4, 0, 0, 0 };

	while (av_read_frame(pFormatCtx, pPacket) >= 0) {
		if (pPacket->stream_index == videoStreamIdx) {
			if (avcodec_send_packet(pCodecCtx, pPacket) == 0) {
				while (avcodec_receive_frame(pCodecCtx, pFrame) == 0) {
					std::cout << "Decoded Frame: PTS " << pFrame->pts << std::endl;

					sws_scale(sws_ctx,
						pFrame->data, pFrame->linesize, 0, pCodecCtx->height,
						dest, dest_linesize);

					SavePPM(buffer.data(), pCodecCtx->width, pCodecCtx->height, 0);

					goto cleanup;
				}
			}
		}
	}

cleanup:
	av_packet_unref(pPacket);
	av_packet_free(&pPacket);
	av_frame_free(&pFrame);
	avcodec_free_context(&pCodecCtx);
	avformat_close_input(&pFormatCtx);
	sws_freeContext(sws_ctx);

	std::cout << "Done. Check the ppm file." << std::endl;
	return 0;
}

// 프로그램 실행: <Ctrl+F5> 또는 [디버그] > [디버깅하지 않고 시작] 메뉴
// 프로그램 디버그: <F5> 키 또는 [디버그] > [디버깅 시작] 메뉴

// 시작을 위한 팁: 
//   1. [솔루션 탐색기] 창을 사용하여 파일을 추가/관리합니다.
//   2. [팀 탐색기] 창을 사용하여 소스 제어에 연결합니다.
//   3. [출력] 창을 사용하여 빌드 출력 및 기타 메시지를 확인합니다.
//   4. [오류 목록] 창을 사용하여 오류를 봅니다.
//   5. [프로젝트] > [새 항목 추가]로 이동하여 새 코드 파일을 만들거나, [프로젝트] > [기존 항목 추가]로 이동하여 기존 코드 파일을 프로젝트에 추가합니다.
//   6. 나중에 이 프로젝트를 다시 열려면 [파일] > [열기] > [프로젝트]로 이동하고 .sln 파일을 선택합니다.
