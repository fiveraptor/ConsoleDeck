#define CLK 5
#define DT 4
#define SW 3

const int buttonPins[] = {6, 7, 8, 9, 10, 11, 12, A0, A1, 2};  // top-left to bottom-right, last = MEDIA

const unsigned long DEBOUNCE_MS = 500;

int counter = 0;
int currentStateCLK;
int lastStateCLK;

unsigned long lastPressTime[10] = {0};
unsigned long lastMuteTime = 0;

void setup() {
  Serial.begin(9600);

  pinMode(CLK, INPUT);
  pinMode(DT, INPUT);
  pinMode(SW, INPUT_PULLUP);
  lastStateCLK = digitalRead(CLK);

  for (int i = 0; i < 10; i++) {
    pinMode(buttonPins[i], INPUT_PULLUP);
  }
}

void loop() {
  // Handshake: respond to "WHO" with device identity
  if (Serial.available() > 0) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd == "WHO") {
      Serial.println("CONSOLEDECK");
      return;
    }
  }

  // Encoder rotation
  currentStateCLK = digitalRead(CLK);
  if (currentStateCLK != lastStateCLK) {
    if (digitalRead(DT) != currentStateCLK) {
      counter++;
    } else {
      counter--;
    }
    Serial.print("VOLUME_");
    Serial.println(counter);
  }
  lastStateCLK = currentStateCLK;

  // Encoder button = MUTE
  if (digitalRead(SW) == LOW) {
    unsigned long now = millis();
    if (now - lastMuteTime >= DEBOUNCE_MS) {
      lastMuteTime = now;
      Serial.println("MUTE");
    }
  }

  // User buttons
  for (int i = 0; i < 10; i++) {
    if (digitalRead(buttonPins[i]) == LOW) {
      unsigned long now = millis();
      if (now - lastPressTime[i] >= DEBOUNCE_MS) {
        lastPressTime[i] = now;
        if (i < 9) {
          Serial.print("BUTTON_");
          Serial.println(i + 1);
        } else {
          Serial.println("MEDIA");
        }
      }
    }
  }
}
