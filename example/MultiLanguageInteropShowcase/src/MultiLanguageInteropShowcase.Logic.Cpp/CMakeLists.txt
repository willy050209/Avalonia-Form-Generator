cmake_minimum_required(VERSION 3.16)
project(MultiLanguageInteropShowcase.Logic.Cpp LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(MultiLanguageInteropShowcase.Logic.Cpp SHARED CppNativeMathService.cpp)

set_target_properties(MultiLanguageInteropShowcase.Logic.Cpp PROPERTIES
    OUTPUT_NAME "MultiLanguageInteropShowcase.Logic.Cpp"
    PREFIX ""
)
