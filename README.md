# Arduino System Display

These are the source code for both the C# windows app and arduino to display a computer's CPU and Memory percentage on an LCD display.

Contributions are welcomed!

Each project has it's own README.

# Serial Comunication (Advance)

If you want to build/code one of the sides youself (PC or Arduino), the serial communication is made of a simple message system. Every message starts with a byte that indicates the command, followed by a variable length of bytes depending on the command. This will get expanded upon as more and more features are added.

Command 100 followed by an integer indicates the new refresh rate the arduino needs to update the display at.

Command 101 followed by a byte for each of the following modules: CPU, Memory, OS Disk Usage, OS Disk I/O. An aditional byte is send currently reserved for the GPU, currently only sends constant 0.
