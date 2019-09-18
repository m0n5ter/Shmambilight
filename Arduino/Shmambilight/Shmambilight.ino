#include <bitswap.h>
#include <chipsets.h>
#include <color.h>
#include <colorpalettes.h>
#include <colorutils.h>
#include <controller.h>
#include <cpp_compat.h>
#include <dmx.h>
#include <FastLED.h>
#include <fastled_config.h>
#include <fastled_delay.h>
#include <fastled_progmem.h>
#include <fastpin.h>
#include <fastspi.h>
#include <fastspi_bitbang.h>
#include <fastspi_dma.h>
#include <fastspi_nop.h>
#include <fastspi_ref.h>
#include <fastspi_types.h>
#include <hsv2rgb.h>
#include <led_sysdefs.h>
#include <lib8tion.h>
#include <noise.h>
#include <pixelset.h>
#include <pixeltypes.h>
#include <platforms.h>
#include <power_mgt.h>

///// User definitions /////

#define PIN 6
#define serialRate 115200

CRGB* _leds = 0;

void setup()
{
  pinMode(LED_BUILTIN, OUTPUT);
  Serial.begin(serialRate);
}

void blink(uint8_t n)
{
  if (n == 0)
    return;

  if (n == 1)
  {
    digitalWrite(LED_BUILTIN, HIGH);
    delay(50);
    digitalWrite(LED_BUILTIN, LOW);
  }
    
  for(uint8_t i = 0; i < n - 1; i++)
  {
    delay(50);
    digitalWrite(LED_BUILTIN, HIGH);
    
    delay(50);
    digitalWrite(LED_BUILTIN, LOW);
  }
}

void loop() 
{ 
  uint8_t prefix[] = {'L', 'E', 'D', 'S'};
  
  for (uint8_t i = 0; i < sizeof prefix; i++)
  {
    while (!Serial.available()) ;;

    if (Serial.read() != prefix[i])
      return;
  }

  while (!Serial.available()) ;;
  uint8_t numLeds = Serial.read();

  if (numLeds > 0)
  {
    if (_leds == 0)
    {
      _leds = new CRGB[numLeds];
      FastLED.addLeds<WS2812, PIN, GRB>(_leds, numLeds);
    }

    for (uint8_t i = 0; i < numLeds; i++)
    {    
      while(!Serial.available());
      _leds[i].r = Serial.read();
  
      while(!Serial.available());
      _leds[i].g = Serial.read();
      
      while(!Serial.available());
      _leds[i].b = Serial.read();
    }
  
    FastLED.show();
  }
  
  Serial.write("OK");
}
