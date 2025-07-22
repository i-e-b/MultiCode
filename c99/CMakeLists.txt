cmake_minimum_required(VERSION 4.0)
project(c99 C)

set(CMAKE_C_STANDARD 99)

add_executable(c99 main.c
        MultiCode.h
        MultiCode.c)
