﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace NintrollerLib
{
    public struct WiiGuitar : INintrollerState
    {
        private bool SpecialButtonSelect => wiimote.buttons.A;
        private bool SpecialButtonTiltCalibMin => wiimote.buttons.One;
        private bool SpecialButtonTiltCalibMax => wiimote.buttons.Two;
        private bool SpecialButtonTouchOn => wiimote.buttons.Plus;
        private bool SpecialButtonTouchOff => wiimote.buttons.Minus;
        private bool SpecialButtonDebugDump => wiimote.buttons.Home;

#if DEBUG
        private bool _debugViewActive;
        public bool DebugViewActive
        {
            get
            {
                return _debugViewActive;
            }
            set
            {
                _debugViewActive = value;
            }
        }
#endif

        public Wiimote wiimote;
        public Joystick Joy;
        public bool G, R, Y, B, O;
        public bool Up, Down, Left, Right;
        public bool Plus, Minus;

        public bool IsGH3;
        public bool IsGH3SetYet { get; private set; }

        private byte oldTouchStripValue;

        public float WhammyHigh;
        public float WhammyLow;

        public float TiltHigh;
        public float TiltLow;

#if DEBUG
        public byte[] DebugLastData;
#endif

#if DEBUG
        private bool DebugButton_Dump;
#endif

        private byte CALIB_Whammy_Min;
        private byte CALIB_Whammy_Max;

        public WiiGuitar(Wiimote wm)
        {
            this = new WiiGuitar();
            wiimote = wm;

            CALIB_Whammy_Min = 0xFF;
            CALIB_Whammy_Max = 0;

            CALIB_Enable_TouchStrip = false;

            oldTouchStripValue = WGT_TOUCH_STRIP_None;

            IsGH3SetYet = false;
            IsGH3 = false;

            CALIB_Tilt_Neutral = 0;
            CALIB_Tilt_TiltedREEEE = (float)(Math.PI / 2);

#if DEBUG
            DebugLastData = new byte[] { 0 };
#endif

            Joy.Calibrate(Calibrations.Defaults.WiiGuitarDefault.Joy);
        }

        public bool Start
        {
            get { return Plus; }
            set { Plus = value; }
        }

        public bool Select
        {
            get { return Minus; }
            set { Minus = value; }
        }

        public bool CALIB_Enable_TouchStrip;

        public float CALIB_Tilt_Neutral;
        public float CALIB_Tilt_TiltedREEEE;
        public float CALIB_Tilt_StartingZ;

        private const byte WGT_TOUCH_STRIP_None = 0x0F;
        private const byte WGT_TOUCH_STRIP_Green = 0x04;
        private const byte WGT_TOUCH_STRIP_Green2 = 0x05;
        private const byte WGT_TOUCH_STRIP_GreenToRed = 0x06;
        private const byte WGT_TOUCH_STRIP_GreenToRed2 = 0x07;
        private const byte WGT_TOUCH_STRIP_GreenToRed3 = 0x08;
        private const byte WGT_TOUCH_STRIP_GreenToRed4 = 0x09;
        private const byte WGT_TOUCH_STRIP_Red = 0x0A;
        private const byte WGT_TOUCH_STRIP_Red2 = 0x0B;
        private const byte WGT_TOUCH_STRIP_Red3 = 0x0C;
        private const byte WGT_TOUCH_STRIP_RedToYellow = 0x0D;
        private const byte WGT_TOUCH_STRIP_RedToYellow2 = 0x0E;
        //private const byte WGT_TOUCH_STRIP_RedToYellow3 = 0x0F; //conflicts with None
        private const byte WGT_TOUCH_STRIP_RedToYellow4 = 0x10;
        private const byte WGT_TOUCH_STRIP_RedToYellow5 = 0x11;
        private const byte WGT_TOUCH_STRIP_Yellow = 0x12;
        private const byte WGT_TOUCH_STRIP_Yellow2 = 0x13;
        private const byte WGT_TOUCH_STRIP_YellowToBlue = 0x14;
        private const byte WGT_TOUCH_STRIP_YellowToBlue2 = 0x15;
        private const byte WGT_TOUCH_STRIP_YellowToBlue3 = 0x16;
        private const byte WGT_TOUCH_STRIP_Blue = 0x17;
        private const byte WGT_TOUCH_STRIP_Blue2 = 0x18;
        private const byte WGT_TOUCH_STRIP_Blue3 = 0x19;
        private const byte WGT_TOUCH_STRIP_BlueToOrange = 0x1A;
        private const byte WGT_TOUCH_STRIP_BlueToOrange2 = 0x1B;
        private const byte WGT_TOUCH_STRIP_BlueToOrange3 = 0x1C;
        private const byte WGT_TOUCH_STRIP_BlueToOrange4 = 0x1D;
        private const byte WGT_TOUCH_STRIP_BlueToOrange5 = 0x1E;
        private const byte WGT_TOUCH_STRIP_Orange = 0x1F;

        private const float WGT_JOY_DIGITAL_THRESH = 0.5f;

        //private const byte WGT_WHAMMY_MIN = 0x10;
        //private const byte WGT_WHAMMY_MAX = 0x1B;

        private static float _MapRange(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

        public void Update(byte[] data)
        {
#if DEBUG
            //DebugLastData = new byte[data.Length];

            //for (int i = 0; i < data.Length; i++)
            //{
            //    DebugLastData[i] = data[i];
            //}

            DebugLastData = data;
#endif

            int offset = 0;
            switch ((InputReport)data[0])
            {
                case InputReport.BtnsExt:
                case InputReport.BtnsExtB:
                    offset = 3;
                    break;
                case InputReport.BtnsAccExt:
                    offset = 6;
                    break;
                case InputReport.BtnsIRExt:
                    offset = 13;
                    break;
                case InputReport.BtnsAccIRExt:
                    offset = 16;
                    break;
                case InputReport.ExtOnly:
                    offset = 1;
                    break;
                default:
                    return;
            }

            if (offset > 0)
            {
                if (!IsGH3SetYet)
                {
                    IsGH3 = (data[offset] & 0x80) == 0x80; //0b10000000
                    IsGH3SetYet = true;
                }

                //Console.Write("WII GUITAR DATA: ");
                //for (int i = offset; i < data.Length; i++)
                //    Console.Write(data[i].ToString("X2") + " ");

                //Console.WriteLine();

                // Buttons
                G = (data[offset + 5] & 0x10) == 0;
                R = (data[offset + 5] & 0x40) == 0;
                Y = (data[offset + 5] & 0x08) == 0;
                B = (data[offset + 5] & 0x20) == 0;
                //L = (data[offset + 4] & 0x20) == 0;
                //R = (data[offset + 4] & 0x02) == 0;
                O = (data[offset + 5] & 0x80) == 0;
                //ZR = (data[offset + 5] & 0x04) == 0;
                Plus = (data[offset + 4] & 0x04) == 0;
                Minus = (data[offset + 4] & 0x10) == 0;
                Minus |= (data[offset + 5] & 0x04) == 0; // Foot pedal jack
                //Home = (data[offset + 4] & 0x08) == 0;

                // Dpad
                Up = (data[offset + 5] & 0x01) == 0;
                Down = (data[offset + 4] & 0x40) == 0;
                //Left = (data[offset + 5] & 0x02) == 0;
                //Right = (data[offset + 4] & 0x80) == 0;

                //Up = false;
                //Down = false;
                Left = false;
                Right = false;

                if (data[offset] != 0 || data[offset + 1] != 0)
                {
                    // Joysticks
                    Joy.rawX = (byte)(data[offset] & 0x3F);
                    Joy.rawY = (byte)(data[offset + 1] & 0x03F);

                    if (Joy.rawX > Joy.maxX)
                        Joy.maxX = Joy.rawX;
                    else if (Joy.rawX < Joy.minX)
                        Joy.minX = Joy.rawX;


                    if (Joy.rawY > Joy.maxY)
                        Joy.maxY = Joy.rawY;
                    else if (Joy.rawY < Joy.minY)
                        Joy.minY = Joy.rawY;


                    Joy.Normalize();

                    bool isJoyPressed = (((Joy.X * Joy.X) + (Joy.Y * Joy.Y)) >= (WGT_JOY_DIGITAL_THRESH * WGT_JOY_DIGITAL_THRESH));
                    double joyDirection = (int)((Math.Atan2(Joy.Y, Joy.X) + (Math.PI / 2)) / (Math.PI / 8));
                    int joyDirStep = (int)(Math.Abs(joyDirection));

                    if (isJoyPressed)
                    {
                        if (joyDirection < 0)
                        {
                            switch (joyDirStep)
                            {
                                case 0: //N
                                    Down = true;
                                    break;
                                case 1: //NE
                                case 2: //NE
                                    Down = true;
                                    Left = true;
                                    break;
                                case 3: //E
                                case 4: //E
                                    Left = true;
                                    break;
                                case 5: //SE
                                case 6: //SE
                                    Left = true;
                                    Up = true;
                                    break;
                                case 7: //S
                                case 8: //S
                                    Up = true;
                                    break;
                                case 9: //SW
                                case 10: //SW
                                    Up = true;
                                    Right = true;
                                    break;
                                case 11: //W
                                case 12: //W
                                    Right = true;
                                    break;

                            }
                        }
                        else
                        {
                            switch (joyDirStep)
                            {
                                case 0: //N
                                    Down = true;
                                    break;
                                case 1: //NW
                                case 2: //NW
                                    Down = true;
                                    Right = true;
                                    break;
                                case 3: //W
                                case 4: //W
                                    Right = true;
                                    break;
                                case 5: //SW
                                case 6: //SW
                                    Right = true;
                                    Up = true;
                                    break;
                                case 7: //S
                                case 8: //S
                                    Up = true;
                                    break;
                                case 9: //SE
                                case 10: //SE
                                    Up = true;
                                    Left = true;
                                    break;
                                case 11: //E
                                case 12: //E
                                    Left = true;
                                    break;
                            }
                        }


                    }
                }



                if ((!IsGH3) && CALIB_Enable_TouchStrip)
                {
                    if (G || R || Y || B || O)
                    {
                        if (data[offset + 2] != WGT_TOUCH_STRIP_None && oldTouchStripValue == WGT_TOUCH_STRIP_None)
                        {
                            Down = true;
                        }
                    }
                    else
                    {
                        switch (data[offset + 2] & 0x1F)
                        {
                            case WGT_TOUCH_STRIP_Green:
                            case WGT_TOUCH_STRIP_Green2:
                                G = true;
                                break;
                            case WGT_TOUCH_STRIP_GreenToRed:
                            case WGT_TOUCH_STRIP_GreenToRed2:
                            case WGT_TOUCH_STRIP_GreenToRed3:
                            case WGT_TOUCH_STRIP_GreenToRed4:
                                G = true;
                                R = true;
                                break;
                            case WGT_TOUCH_STRIP_Red:
                            case WGT_TOUCH_STRIP_Red2:
                            case WGT_TOUCH_STRIP_Red3:
                                R = true;
                                break;
                            case WGT_TOUCH_STRIP_RedToYellow:
                            case WGT_TOUCH_STRIP_RedToYellow2:
                            //case WGT_TOUCH_STRIP_RedToYellow3: //conflicts with None
                            case WGT_TOUCH_STRIP_RedToYellow4:
                            case WGT_TOUCH_STRIP_RedToYellow5:
                                R = true;
                                Y = true;
                                break;
                            case WGT_TOUCH_STRIP_Yellow:
                            case WGT_TOUCH_STRIP_Yellow2:
                                Y = true;
                                break;
                            case WGT_TOUCH_STRIP_YellowToBlue:
                            case WGT_TOUCH_STRIP_YellowToBlue2:
                            case WGT_TOUCH_STRIP_YellowToBlue3:
                                Y = true;
                                B = true;
                                break;
                            case WGT_TOUCH_STRIP_Blue:
                            case WGT_TOUCH_STRIP_Blue2:
                            case WGT_TOUCH_STRIP_Blue3:
                                B = true;
                                break;
                            case WGT_TOUCH_STRIP_BlueToOrange:
                            case WGT_TOUCH_STRIP_BlueToOrange2:
                            case WGT_TOUCH_STRIP_BlueToOrange3:
                            case WGT_TOUCH_STRIP_BlueToOrange4:
                            case WGT_TOUCH_STRIP_BlueToOrange5:
                                B = true;
                                O = true;
                                break;
                            case WGT_TOUCH_STRIP_Orange:
                                O = true;
                                break;
                        }
                    }

                    oldTouchStripValue = data[offset + 2];
                }

                //// Normalize
                //Joy.Normalize();

                //if (Joy.Y > 0.7f)
                //    Up = true;
                //else if (Joy.Y < -0.7f)
                //    Down = true;

                //Left = Joy.X < -0.7f;
                //Right = Joy.X > 0.7f;

                byte currentWhammyValue = (byte)(data[offset + 3] & 0x1F);

                if (currentWhammyValue < CALIB_Whammy_Min)
                    CALIB_Whammy_Min = currentWhammyValue;

                if (currentWhammyValue > CALIB_Whammy_Max)
                    CALIB_Whammy_Max = currentWhammyValue;

                float whammy = (2.0f * (1.0f * (currentWhammyValue - CALIB_Whammy_Min) / (CALIB_Whammy_Max - CALIB_Whammy_Min)) - 1);

                WhammyHigh = Math.Max(whammy, 0);
                WhammyLow = Math.Min(whammy, 0);


                //Console.Write("WII GUITAR:");
                //Console.Write($"Frets:{(A ? "_" : "-")}{(B ? "_" : "-")}{(X ? "_" : "-")}{(Y ? "_" : "-")}{(ZL ? "_" : "-")}");

                //Console.Write($"    Joy1=[{LJoy.X},{LJoy.Y}]    ");
                //Console.Write($"Joy2=[{RJoy.X},{RJoy.Y}]    ");

                //Console.WriteLine();


            }

#if LOW_BANDWIDTH

#else
            wiimote.Update(data);

            // Wiimote is sideways so these are weird
            if (wiimote.buttons.Up)
                Left = true;
            else if (wiimote.buttons.Down)
                Right = true;

            if (wiimote.buttons.Right)
                Down = true;
            else if (wiimote.buttons.Left)
                Up = true;

            // A on the actual wiimote
            if (SpecialButtonSelect)
                Select = true;

            // Tilt calibration min / resting pose (+ initialize)
            if (SpecialButtonTiltCalibMin)
            {
                wiimote.accelerometer.centerX = wiimote.accelerometer.rawX;
                wiimote.accelerometer.centerY = wiimote.accelerometer.rawY;
                wiimote.accelerometer.centerZ = wiimote.accelerometer.rawZ - 32;

                wiimote.accelerometer.minX = wiimote.accelerometer.centerX - 32;
                wiimote.accelerometer.maxX = wiimote.accelerometer.centerX + 32;

                wiimote.accelerometer.minY = wiimote.accelerometer.centerY - 32;
                wiimote.accelerometer.maxY = wiimote.accelerometer.centerY + 32;

                wiimote.accelerometer.minZ = wiimote.accelerometer.centerZ - 32;
                wiimote.accelerometer.maxZ = wiimote.accelerometer.centerZ + 32;

                wiimote.accelerometer.deadX = 0;
                wiimote.accelerometer.deadY = 0;
                wiimote.accelerometer.deadZ = 0;

                wiimote.accelerometer.Normalize();

                CALIB_Tilt_StartingZ = 0;

                CALIB_Tilt_Neutral = 0;// (float)Math.Atan2(Math.Abs(wiimote.accelerometer.Y), Math.Abs(wiimote.accelerometer.X)) * Math.Max(Math.Min(1 - Math.Abs(wiimote.accelerometer.Z - CALIB_Tilt_StartingZ), 1), 0);

                CALIB_Tilt_TiltedREEEE = (float)(CALIB_Tilt_Neutral + Math.PI / 2);
            }

            float tiltAngle = (float)Math.Atan2(Math.Abs(wiimote.accelerometer.Y),
                Math.Abs(wiimote.accelerometer.X)) *
                Math.Max(Math.Min(1 - Math.Abs(wiimote.accelerometer.Z - CALIB_Tilt_StartingZ), 1), 0);

            // Tilt calibration max
            if (SpecialButtonTiltCalibMax)
                CALIB_Tilt_TiltedREEEE = tiltAngle;


            float tilt = _MapRange(tiltAngle, CALIB_Tilt_Neutral, CALIB_Tilt_TiltedREEEE, 0, 1);

            TiltHigh = Math.Min(Math.Max(tilt, 0), 1);
            TiltLow = Math.Max(Math.Min(tilt, 0), -1);

            if (!IsGH3)
            {
                if (SpecialButtonTouchOn && !CALIB_Enable_TouchStrip)
                    CALIB_Enable_TouchStrip = true;
                else if (SpecialButtonTouchOff && CALIB_Enable_TouchStrip)
                    CALIB_Enable_TouchStrip = false;
            }
#endif






#if DEBUG
            if (offset > 0)
            {
                if (SpecialButtonDebugDump)
                {
                    if (!DebugButton_Dump)
                    {
                        DebugButton_Dump = true;

                        //var sb = new StringBuilder();

                        //sb.AppendLine("Wii Guitar data packet dump:");

                        //for (int i = 0; i < data.Length; i++)
                        //{
                        //    sb.Append(data[i].ToString("X2") + " ");
                        //}

                        //MessageBox.Show(sb.ToString(), "DEBUG: WII GUITAR DUMP", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        DebugViewActive = true;
                    }
                }
                else
                {
                    DebugButton_Dump = false;
                }

                
            }
#endif

        }

        public float GetValue(string input)
        {
            throw new NotImplementedException();
        }

        public void SetCalibration(Calibrations.CalibrationPreset preset)
        {
            wiimote.SetCalibration(preset);

            //switch (preset)
            //{
            //    case Calibrations.CalibrationPreset.Default:
            //        //LJoy.Calibrate(Calibrations.Defaults.ClassicControllerProDefault.LJoy);
            //        //RJoy.Calibrate(Calibrations.Defaults.ClassicControllerProDefault.RJoy);
            //        SetCalibration(Calibrations.Defaults.ClassicControllerProDefault);
            //        break;

            //    case Calibrations.CalibrationPreset.Modest:
            //        SetCalibration(Calibrations.Moderate.ClassicControllerProModest);
            //        break;

            //    case Calibrations.CalibrationPreset.Extra:
            //        SetCalibration(Calibrations.Extras.ClassicControllerProExtra);
            //        break;

            //    case Calibrations.CalibrationPreset.Minimum:
            //        SetCalibration(Calibrations.Minimum.ClassicControllerProMinimal);
            //        break;

            //    case Calibrations.CalibrationPreset.None:
            //        SetCalibration(Calibrations.None.ClassicControllerProRaw);
            //        break;
            //}



            Joy.Calibrate(Calibrations.Defaults.WiiGuitarDefault.Joy);

            //SetCalibration(Calibrations.Defaults.ClassicControllerProDefault);
        }

        public void SetCalibration(INintrollerState from)
        {
            //if (from.CalibrationEmpty)
            //{
            //    // don't apply empty calibrations
            //    return;
            //}

            //if (from.GetType() == typeof(WiiGuitar))
            //{
            //    Joy.Calibrate(((WiiGuitar)from).Joy);
            //}
            //else if (from.GetType() == typeof(ClassicControllerPro))
            //{
            //    Joy.Calibrate(((ClassicControllerPro)from).LJoy);
            //}
            //else if (from.GetType() == typeof(Wiimote))
            //{
            //    wiimote.SetCalibration(from);
            //}
        }

        public void SetCalibration(string calibrationString)
        {
            if (calibrationString.Count(c => c == '0') > 5)
            {
                // don't set empty calibrations
                return;
            }

            string[] components = calibrationString.Split(new char[] { ':' });

            foreach (string component in components)
            {
                if (component.StartsWith("joy"))
                {
                    string[] joyLConfig = component.Split(new char[] { '|' });

                    for (int jL = 1; jL < joyLConfig.Length; jL++)
                    {
                        int value = 0;
                        if (int.TryParse(joyLConfig[jL], out value))
                        {
                            switch (jL)
                            {
                                case 1: Joy.centerX = value; break;
                                case 2: Joy.minX = value; break;
                                case 3: Joy.maxX = value; break;
                                case 4: Joy.deadX = value; break;
                                case 5: Joy.centerY = value; break;
                                case 6: Joy.minY = value; break;
                                case 7: Joy.maxY = value; break;
                                case 8: Joy.deadY = value; break;
                                default: break;
                            }
                        }
                    }
                }
            }
        }

        public string GetCalibrationString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("-wgt");
            sb.Append(":joy");
            sb.Append("|"); sb.Append(Joy.centerX);
            sb.Append("|"); sb.Append(Joy.minX);
            sb.Append("|"); sb.Append(Joy.maxX);
            sb.Append("|"); sb.Append(Joy.deadX);
            sb.Append("|"); sb.Append(Joy.centerY);
            sb.Append("|"); sb.Append(Joy.minY);
            sb.Append("|"); sb.Append(Joy.maxY);
            sb.Append("|"); sb.Append(Joy.deadY);

            return sb.ToString();
        }

        public bool CalibrationEmpty
        {
            get
            {
                if (Joy.maxX == 0 && Joy.maxY == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public IEnumerator<KeyValuePair<string, float>> GetEnumerator()
        {
            foreach (var input in wiimote)
            {
                yield return input;
            }

            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.G, G ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.R, R ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.Y, Y ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.B, B ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.O, O ? 1.0f : 0.0f);

            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.UP, (Up ? 1.0f : 0.0f));
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.DOWN, (Down ? 1.0f : 0.0f));
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.LEFT, (Left ? 1.0f : 0.0f));
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.RIGHT, (Right ? 1.0f : 0.0f));

            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.START, Start ? 1.0f : 0.0f);
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.SELECT, Select ? 1.0f : 0.0f);

            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.WHAMMYHIGH, WhammyHigh);
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.WHAMMYLOW, WhammyLow);

            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.TILTHIGH, TiltHigh);
            yield return new KeyValuePair<string, float>(INPUT_NAMES.WII_GUITAR.TILTLOW, TiltLow);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
