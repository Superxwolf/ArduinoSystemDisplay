// Arduino System Display for LCD1602
#include <LiquidCrystal.h>// include the library code

int refresh_rate = 250; // Initial refresh rate, configurable from the PC app.

LiquidCrystal lcd(4, 6, 10, 11, 12, 13);

/*********************************************************/
byte serial_buffer[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

byte cur_cpu = 0;
byte cur_memory = 0;

void setup()
{
  lcd.begin(16, 2); // set up the LCD's number of columns and rows

  // PIN 3 is connected to V0 on the LCD and set to 100 which is a good contrast value.
  pinMode(3, OUTPUT);
  analogWrite(3, 100);
  
  Serial.begin(9600);
}

void printAt(String str, int startX, int startY)
{
  for(int i = 0; i < str.length(); i++)
  {
    lcd.setCursor(startX + i,startY);
    lcd.print(str.charAt(i));
  }
}
/*********************************************************/
void loop()
{
  while(Serial.available() > 0)
  {
    byte command = Serial.read();

    switch(command)
    {
      // Update refresh rate
      // Computer sends an int which will be the new refresh rate
      case 100:
      {
        Serial.readBytes(serial_buffer, 4);
        refresh_rate = *(int *)serial_buffer;
        break;
      }
        
      // Update CPU and Memory
      // Computer sends a byte per component, already in percentage range.
      case 101:
        Serial.readBytes(serial_buffer, 5);
        cur_cpu = serial_buffer[0];
        cur_memory = serial_buffer[1];
        break;
    }
  }

  lcd.clear();
  printAt("CPU", 1, 0);
  printAt("Memory", 1, 1);

  String cpu_str = String(cur_cpu);
  cpu_str = cpu_str + "%";

  // Print CPU value with calculated right align
  printAt(cpu_str, 11 + (3 - cpu_str.length()), 0);

  String mem_str = String(cur_memory);
  mem_str = mem_str + "%";

  // Print Memory value with calculated right align
  printAt(mem_str, 11 + (3 - mem_str.length()), 1);

  delay(refresh_rate);
}
/************************************************************/
