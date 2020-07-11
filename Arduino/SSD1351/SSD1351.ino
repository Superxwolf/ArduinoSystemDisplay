#include "OLED_Driver.h"
#include "OLED_GFX.h"

#include <SPI.h>
#include <Wire.h>

#define TEXT_HEIGHT 8
#define BAR_SPACING 2
#define BAR_HEIGHT 2
#define BLOCK_SPACING 15
#define TOTAL_BLOCK_HEIGHT (TEXT_HEIGHT+BAR_SPACING+BAR_HEIGHT+BLOCK_SPACING)
#define GET_BLOCK_OFFSET(num) TOTAL_BLOCK_HEIGHT*num

struct BLOCK
{
  const char* text;
  int cur_value;
  int color;

  BLOCK(const char* _text, int _color)
  {
    text = _text;
    cur_value = 0;
    color = _color;
  }
};

#define BLOCK_COUNT 4
BLOCK blocks[BLOCK_COUNT] = { BLOCK("CPU", WHITE), BLOCK("Memory", BLUE), BLOCK("Disk", GREEN), BLOCK("Disk I/O", YELLOW)/*, BLOCK("GPU", CYAN)*/ };

OLED_GFX oled = OLED_GFX();

byte serial_buffer[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
int refresh_rate = 500;

void setup() 
{
  //Init GPIO
  pinMode(oled_cs, OUTPUT);
  pinMode(oled_rst, OUTPUT);
  pinMode(oled_dc, OUTPUT);

  //Init UART
  Serial.begin(9600);

#if INTERFACE_4WIRE_SPI
  //Init SPI
  SPI.setDataMode(SPI_MODE0);
  SPI.setBitOrder(MSBFIRST);
  SPI.setClockDivider(SPI_CLOCK_DIV2);
  SPI.begin();

#elif INTERFACE_3WIRE_SPI

  pinMode(oled_sck, OUTPUT);
  pinMode(oled_din, OUTPUT);

#endif

  oled.Device_Init();

  oled.Clear_Screen();
  DrawText();
}

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

        // All values are multiplied by 1.27 to extend to the edge of the screen
        blocks[0].cur_value = serial_buffer[0] * 1.27; // CPU
        blocks[1].cur_value = serial_buffer[1] * 1.27; // Memory
        blocks[2].cur_value = serial_buffer[2] * 1.27; // Disk
        blocks[3].cur_value = serial_buffer[3] * 1.27; // Disk I/O
        //blocks[4].cur_value = serial_buffer[4] * 1.27; // GPU
        break;
    }
  }
  
  int blockOffset;
  int barY;
  
  for(int i = 0; i < BLOCK_COUNT; i++)
  {
    BLOCK cur_block = blocks[i];

    blockOffset = GET_BLOCK_OFFSET(i);
    barY = blockOffset + TEXT_HEIGHT + BAR_SPACING;
  
    oled.Set_Color(BLACK);
    oled.Draw_Rect(0,barY,128,BAR_HEIGHT);

    if(cur_block.cur_value > 0)
    {
      oled.Set_Color(cur_block.color);
      oled.Draw_Rect(0,barY,cur_block.cur_value,BAR_HEIGHT);
    }
  }

  delay(refresh_rate);
}

void ClearText()
{
  oled.Set_Color(BLACK);
  for(int i = 0; i < BLOCK_COUNT; i++)
  {
    oled.print_String(0,GET_BLOCK_OFFSET(i), blocks[i].text, FONT_5X8);
  }
}

void DrawText()
{
  for(int i = 0; i < BLOCK_COUNT; i++)
  {
    BLOCK cur_block = blocks[i];

    oled.Set_Color(cur_block.color);
    oled.print_String(0,GET_BLOCK_OFFSET(i), cur_block.text, FONT_5X8);
  }
}
