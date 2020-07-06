# Arduino System Display

These are the source code for both the C# windows app and arduino to display a computer's CPU and Memory percentage on an LCD display.

Contributions are welcomed!

# PC Side

Currently the app is made for Windows using C#. Upon running is will just be an icon in the system tray. It's context menu has a few options and shows the current status of the connection.

# Arduino Side

Currently only supports the LCD1602. Followed the following [wiring tutorial](http://wiki.sunfounder.cc/index.php?title=LCD1602_Module), with the exception of replacing the potentiometer with PIN 3 and settings it to analogWrite of 100 (using PWM).

# Serial Comunication (Advance)

If you want to build/code one of the sides youself (PC or Arduino), the serial communication is made of a simple message system. Every message starts with a byte that indicates the command, followed by a variable length of bytes depending on the command. This will get expanded upon as more and more features are added.

Command 100 followed by an integer indicates the new refresh rate the arduino needs to update the display at.

Command 101 followed by a byte for CPU percentage value and a byte for Memory percentage value.
