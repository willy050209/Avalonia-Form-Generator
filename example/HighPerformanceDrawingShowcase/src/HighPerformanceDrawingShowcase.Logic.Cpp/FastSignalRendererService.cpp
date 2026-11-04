#include "FastSignalRendererService.h"
#include <cmath>
#include <cstdint>
#include <algorithm>

constexpr double PI = 3.14159265358979323846;

extern "C" {

AFG_API void FastSignalRendererService_ComputeWaveformBuffer(
    int64_t bufferPtr,
    int32_t width,
    int32_t height,
    double frequency,
    double amplitude,
    double phase)
{
    if (bufferPtr == 0 || width <= 0 || height <= 0) return;

    auto* pixels = reinterpret_cast<uint32_t*>(bufferPtr);

    const uint32_t ColorBg = 0xFF0A0F1D;
    const uint32_t ColorCenterAxis = 0xFF1C2C3D;
    const uint32_t ColorGridMajor = 0xFF121B28;
    const uint32_t ColorTick = 0xFF2B4158;
    const uint32_t ColorWaveCore = 0xFF4ADE80;
    const uint32_t ColorWaveGlow1 = 0xFF22C55E;
    const uint32_t ColorWaveGlow2 = 0xFF15803D;

    const int32_t centerX = width / 2;
    const int32_t centerY = height / 2;

    // 1. 背景與座標網格
    for (int32_t y = 0; y < height; ++y) {
        int32_t rowOffset = y * width;
        bool isCenterY = (y == centerY);
        bool isGridY = (y % 50 == 0);

        for (int32_t x = 0; x < width; ++x) {
            bool isCenterX = (x == centerX);
            bool isGridX = (x % 50 == 0);

            uint32_t color = ColorBg;
            if (isCenterY || isCenterX) {
                if ((isCenterY && (x % 10 == 0)) || (isCenterX && (y % 10 == 0))) {
                    color = ColorTick;
                } else {
                    color = ColorCenterAxis;
                }
            } else if (isGridY || isGridX) {
                color = ColorGridMajor;
            }

            pixels[rowOffset + x] = color;
        }
    }

    // 2. 向量化波形取樣與連線繪製
    auto calcY = [=](int32_t x) -> int32_t {
        double normalizedX = static_cast<double>(x) / static_cast<double>(width);
        double theta = 2.0 * PI * frequency * normalizedX + phase;
        double signal = std::sin(theta);
        int32_t sampleY = static_cast<int32_t>(std::round(centerY - amplitude * signal));
        return std::clamp(sampleY, 2, height - 3);
    };

    int32_t prevY = calcY(0);

    for (int32_t x = 0; x < width; ++x) {
        int32_t curY = calcY(x);
        int32_t minY = std::min(prevY, curY);
        int32_t maxY = std::max(prevY, curY);

        for (int32_t y = minY; y <= maxY; ++y) {
            if (y >= 0 && y < height) {
                pixels[y * width + x] = ColorWaveCore;

                if (y - 1 >= 0 && pixels[(y - 1) * width + x] != ColorWaveCore)
                    pixels[(y - 1) * width + x] = ColorWaveGlow1;
                if (y + 1 < height && pixels[(y + 1) * width + x] != ColorWaveCore)
                    pixels[(y + 1) * width + x] = ColorWaveGlow1;
                if (y - 2 >= 0 && pixels[(y - 2) * width + x] != ColorWaveCore)
                    pixels[(y - 2) * width + x] = ColorWaveGlow2;
                if (y + 2 < height && pixels[(y + 2) * width + x] != ColorWaveCore)
                    pixels[(y + 2) * width + x] = ColorWaveGlow2;
            }
        }
        prevY = curY;
    }
}

}
