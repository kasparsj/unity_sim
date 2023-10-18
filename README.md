# Unity Simulation

Unity simulation environment for AGH Space Systems robotics projects.

![](./docs/cover.png)

## Getting Started

This repository uses Git LFS extension to facilitate version control of large asset files in this project. **BEFORE YOU CLONE** this repository, you must install the Git LFS addon using your system's package manager or by downloading it directly as described [here](https://docs.github.com/en/repositories/working-with-files/managing-large-files/installing-git-large-file-storage). This hyperlink also contains instructions on how to verify if the extension works correctly.

Once Git LFS works on your system, please clone the repository to your ROS 2 workspace:
```bash
cd src
git clone git@github.com:agh-space-systems-rover/unity_sim.git
cd ..
colcon build
source install/setup.bash
```

Once the workspace is built, you need to find out which Unity version is required by this project:
```bash
ros2 run unity_sim version
```
And this command will yield something akin to `Unity 2023.1.9f1`. (Exact version may differ!)

You need to install this version of Unity on your system. Start by installing Unity Hub (either with the AUR package `unityhub` or) by downloading it directly from [here](https://unity3d.com/get-unity/download). Once you have Unity Hub installed, it should prompt you to sign into your account in order to activate a personal license. When you are logged in, you can install the required version of Unity. Unity Hub only provides download links for the latest versions of Unity, so you will need to use the [Unity Archive](https://unity.com/releases/editor/archive) to find the version you need. From there you can click a button that will open Unity Hub and start the download. After the download is complete, it is best to test if your installation is able to run other demo projects, but you should be fine to close Unity Hub now and run the simulation:
```bash
ros2 launch unity_sim unity_sim.launch.py
```

On the first run, the project will take a while to start, because it needs to download some packages from the web and import all the assets. Subsequent runs however will be much faster. Unity might still take up to a dozen of seconds to start, so you can always use the above command to run the simulation in a new terminal window, while you continue to restart other nodes separately.

If you wish to upgrade the simulation to a newer version of Unity, please launch it using the Unity Hub. The project directory is located [here](./unity_project/unity_sim) and will need to be manually selected in the Unity Hub.

## IMU Simulation

The simulation provides a virtual IMU sensor. It is a standalone [C# script](./unity_sim/Assets/Simulation/IMU/IMU.cs) that can be attached to any GameObject of choice. The sensor works by comparing the subsequent positions of the GameObject between this and previous physics frame. In this way it can recover the velocity. Then the velocity from the previous frame is compared with the current velocity to compute the acceleration. Angular values are calculated in an analogous way. The IMU's topic and report frequency is configurable in the settings of the script component.

## How RealSense Cameras are Simulated

In Unity each RealSense is a prefab composed of a single `GameObject` that contains a camera and the control script. The script renders the camera at a set rate and applies a shader that embeds the depth information in the alpha channel of the image. When a frame is available, it is read by the script onto the CPU, and from there [native C++ code](./unity_sim/unity_rs_publisher_plugin/) in invoked that sends the unmodified binary buffer over a Unix socket to the `unity_rs_publisher` ROS node. The node generates point clouds, image messages, camera info and publishes all this data. Importantly, the node subscribes to `{camera}/unity_rs_publisher/meta` topics to receive the cameras' configurations. Only then the node has all the required information to generate its own messages. 

## RealSense Publisher Plugin Development

This Unity native plugin is a rather simple piece of code that forwards live feed from simulated RealSense cameras, over sockets, to a ROS 2 publisher node.
It is distributed within this repository in binary form. The component is built for the AMD64 Linux platform. You **DO NOT NEED** to install the below components if you do not plan to develop the plugin and only want to use it.

This software must be installed on the host computer in order to build the plugin:
- CMake
- C++ 20 compiler

The plugin will only be recompiled if the simulation [starts](./unity_sim/unity_sim/__init__.py) and the [binary file](./unity_sim/Assets/Simulation/RealSense/UnityRSPublisherPlugin.so) is not found in the assets folder. That is why you need to manually remove it and restart the simulation to recompile the plugin.

