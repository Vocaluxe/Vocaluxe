#!/usr/bin/env python3
"""Copies the GStreamer plugins Vocaluxe needs and all libraries they depend on from the official GStreamer macOS
framework into a self-contained folder (arm64 only):

    <target>/lib/*.dylib
    <target>/lib/gstreamer-1.0/*.dylib      plugins
    <target>/libexec/gstreamer-1.0/gst-plugin-scanner

The framework libraries use @rpath with rpaths relative to themselves (@loader_path/../lib, ...), so this layout works
without changing the install names. The licenses are in Output/Licenses/GStreamer (the framework contains no license files).

Usage: copy-gstreamer.py <framework folder> <target folder> <plugin> [<plugin> ...]
"""
import os
import subprocess
import sys

# libraries that are loaded by name by gstreamer-sharp/glib-sharp, not only as dependencies
EXTRA_LIBS = ["libgstreamer-1.0.0.dylib", "libgstbase-1.0.0.dylib", "libgstcontroller-1.0.0.dylib",
              "libglib-2.0.0.dylib", "libgobject-2.0.0.dylib", "libgio-2.0.0.dylib", "libgstaudio-1.0.0.dylib",
              "libgstpbutils-1.0.0.dylib", "libgstvideo-1.0.0.dylib", "libgsttag-1.0.0.dylib"]


def dependencies(path):
    output = subprocess.run(["otool", "-L", "-arch", "arm64", path], check=True, capture_output=True, text=True).stdout
    for line in output.splitlines()[1:]:
        name = line.strip().split(" ")[0]
        if name.startswith("@rpath/"):
            yield name[len("@rpath/"):]


def main():
    framework, target, plugins = sys.argv[1], sys.argv[2], sys.argv[3:]
    lib = os.path.join(framework, "lib")
    sources = [os.path.join(lib, "gstreamer-1.0", "libgst%s.dylib" % p) for p in plugins]
    sources += [os.path.join(lib, name) for name in EXTRA_LIBS]
    sources.append(os.path.join(framework, "libexec", "gstreamer-1.0", "gst-plugin-scanner"))

    files, todo = set(), list(sources)
    while todo:
        path = todo.pop()
        if path in files:
            continue
        if not os.path.exists(path):
            sys.exit("Missing: " + path)
        files.add(path)
        for name in dependencies(path):
            dependency = os.path.join(lib, name)
            if os.path.exists(dependency):
                todo.append(dependency)

    for path in sorted(files):
        destination = os.path.join(target, os.path.relpath(path, framework))
        os.makedirs(os.path.dirname(destination), exist_ok=True)
        # universal -> arm64 only
        subprocess.run(["lipo", path, "-thin", "arm64", "-output", destination], check=True)
        print("gstreamer/" + os.path.relpath(destination, target))


if __name__ == "__main__":
    main()
