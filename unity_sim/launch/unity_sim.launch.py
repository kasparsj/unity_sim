from launch import LaunchDescription
from launch_ros.actions import Node


def generate_launch_description():
    return LaunchDescription(
        [
            Node(package="unity_sim", executable="simulation"),
            # rosbridge server for C# <-> ROS communication
            Node(package="rosbridge_server", executable="rosbridge_websocket"),
            # custom Unix Socket server for camera image transfer
            Node(package="unity_rs_publisher", executable="unity_rs_publisher"),
            # tf_static republisher
            Node(
                package="unity_tf_static_republisher",
                executable="unity_tf_static_republisher",
            ),
        ]
    )
