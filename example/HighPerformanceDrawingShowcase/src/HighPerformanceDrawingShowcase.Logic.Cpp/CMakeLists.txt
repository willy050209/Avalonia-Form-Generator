cmake_minimum_required(VERSION 3.16)
project(HighPerformanceDrawingShowcase.Logic.Cpp LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(HighPerformanceDrawingShowcase.Logic.Cpp SHARED FastSignalRendererService.cpp)

set_target_properties(HighPerformanceDrawingShowcase.Logic.Cpp PROPERTIES
    OUTPUT_NAME "HighPerformanceDrawingShowcase.Logic.Cpp"
    PREFIX ""
)
