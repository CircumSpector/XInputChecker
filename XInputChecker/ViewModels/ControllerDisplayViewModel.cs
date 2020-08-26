using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.XInput;

namespace XInputChecker.ViewModels
{
    public class ControllerDisplayViewModel
    {
        private ReaderWriterLockSlim refreshLocker = new ReaderWriterLockSlim();

        private Controller[] controllers;
        public Controller[] Controllers { get => controllers; }

        public Controller ActiveController
        {
            get => controllers[slotIndex];
        }

        private int slotIndex = 0;
        public int SlotIndex
        {
            get => slotIndex;
            set
            {
                if (slotIndex == value) return;
                PreSlotIndexChange?.Invoke(this, slotIndex, value);
                slotIndex = value;
                SlotIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler SlotIndexChanged;
        private delegate void ChangingSlotIndexHandler(ControllerDisplayViewModel sender,
            int previous, int next);
        private event ChangingSlotIndexHandler PreSlotIndexChange;


        private StateWrapper stateWrapper = new StateWrapper();
        public StateWrapper StateWrapper { get => stateWrapper; }

        private bool connectionStatus = false;

        public string ConnectStatus
        {
            get => controllers[slotIndex].IsConnected ? "Connected" : "Not Connected";
        }
        public event EventHandler ConnectStatusChanged;

        public string ConnectStatusColor
        {
            get
            {
                string result = "#FF000000";
                if (controllers[slotIndex].IsConnected)
                {
                    result = "#FF0D9100";
                }
                else
                {
                    result = "#FFE60000";
                }

                return result;
            }
        }
        public event EventHandler ConnectStatusColorChanged;

        private ushort leftMotorStrength;
        public ushort LeftMotorStrength
        {
            get => leftMotorStrength;
            set
            {
                if (leftMotorStrength == value) return;
                leftMotorStrength = value;
                LeftMotorStrengthChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler LeftMotorStrengthChanged;

        private ushort rightMotorStrength;
        public ushort RightMotorStrength
        {
            get => rightMotorStrength;
            set
            {
                if (rightMotorStrength == value) return;
                rightMotorStrength = value;
                RightMotorStrengthChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RightMotorStrengthChanged;

        public ControllerDisplayViewModel()
        {
            controllers = new Controller[4]
            {
                new Controller(UserIndex.One), new Controller(UserIndex.Two),
                new Controller(UserIndex.Three), new Controller(UserIndex.Four),
            };

            connectionStatus = controllers[slotIndex].IsConnected;

            PreSlotIndexChange += ControllerDisplayViewModel_PreSlotIndexChange;
            SlotIndexChanged += ControllerDisplayViewModel_SlotIndexChanged;
            LeftMotorStrengthChanged += ControllerDisplayViewModel_LeftMotorStrengthChanged;
            RightMotorStrengthChanged += ControllerDisplayViewModel_RightMotorStrengthChanged;
        }

        private void ControllerDisplayViewModel_RightMotorStrengthChanged(object sender, EventArgs e)
        {
            if (connectionStatus)
            {
                ActiveController.SetVibration(new Vibration() { LeftMotorSpeed = leftMotorStrength, RightMotorSpeed = rightMotorStrength });
            }
        }

        private void ControllerDisplayViewModel_LeftMotorStrengthChanged(object sender, EventArgs e)
        {
            if (connectionStatus)
            {
                ActiveController.SetVibration(new Vibration() { LeftMotorSpeed = leftMotorStrength, RightMotorSpeed = rightMotorStrength });
            }
        }

        private void ControllerDisplayViewModel_PreSlotIndexChange(ControllerDisplayViewModel sender, int previous, int next)
        {
            leftMotorStrength = 0;
            rightMotorStrength = 0;
            if (connectionStatus)
            {
                ActiveController.SetVibration(new Vibration() { LeftMotorSpeed = leftMotorStrength, RightMotorSpeed = rightMotorStrength });
            }
        }

        private void ControllerDisplayViewModel_SlotIndexChanged(object sender, EventArgs e)
        {
            connectionStatus = controllers[slotIndex].IsConnected;
            ConnectStatusChanged?.Invoke(this, EventArgs.Empty);
            ConnectStatusColorChanged?.Invoke(this, EventArgs.Empty);
            BroadcastRumbleState();
        }

        public void UpdateActiveState()
        {
            using (WriteLocker locker = new WriteLocker(refreshLocker))
            {
                stateWrapper.UpdateState(controllers[slotIndex]);
            }

            if (connectionStatus != controllers[slotIndex].IsConnected)
            {
                connectionStatus = controllers[slotIndex].IsConnected;
                ConnectStatusChanged?.Invoke(this, EventArgs.Empty);
                ConnectStatusColorChanged?.Invoke(this, EventArgs.Empty);
                LeftMotorStrength = RightMotorStrength = 0;
            }
        }

        public void BroadcastRumbleState()
        {
            LeftMotorStrengthChanged?.Invoke(this, EventArgs.Empty);
            RightMotorStrengthChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class StateWrapper
    {
        enum DpadDirections : uint
        {
            Centered,
            Up = 1,
            Right = 2,
            UpRight = 3,
            Down = 4,
            DownRight = 6,
            Left = 8,
            UpLeft = 9,
            DownLeft = 12,
        }

        private State currentState;

        public int LX { get => currentState.Gamepad.LeftThumbX; }
        public int LY { get => currentState.Gamepad.LeftThumbY; }
        public int RX { get => currentState.Gamepad.RightThumbX; }
        public int RY { get => currentState.Gamepad.RightThumbY; }
        public int LT { get => currentState.Gamepad.LeftTrigger; }
        public int RT { get => currentState.Gamepad.RightTrigger; }
        public bool A { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.A) != 0; }
        public bool B { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.B) != 0; }
        public bool X { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.X) != 0; }
        public bool Y { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.Y) != 0; }

        public bool LB { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0; }
        public bool RB { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0; }
        public bool ThumbL { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.LeftThumb) != 0; }
        public bool ThumbR { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.RightThumb) != 0; }

        public bool Back { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.Back) != 0; }
        public bool Start { get => (currentState.Gamepad.Buttons & GamepadButtonFlags.Start) != 0; }

        public string DPad
        {
            get
            {
                string result = "Centered";
                DpadDirections currentDir = DpadDirections.Centered;
                if ((currentState.Gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0)
                {
                    currentDir |= DpadDirections.Up;
                }
                if ((currentState.Gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0)
                {
                    currentDir |= DpadDirections.Left;
                }
                if ((currentState.Gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0)
                {
                    currentDir |= DpadDirections.Right;
                }
                if ((currentState.Gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0)
                {
                    currentDir |= DpadDirections.Down;
                }

                switch(currentDir)
                {
                    case DpadDirections.Centered:
                        result = "Centered";
                        break;
                    case DpadDirections.Up:
                        result = "Up";
                        break;
                    case DpadDirections.UpRight:
                        result = "UpRight";
                        break;
                    case DpadDirections.Right:
                        result = "Right";
                        break;
                    case DpadDirections.DownRight:
                        result = "DownRight";
                        break;
                    case DpadDirections.Down:
                        result = "Down";
                        break;
                    case DpadDirections.DownLeft:
                        result = "DownLeft";
                        break;
                    case DpadDirections.Left:
                        result = "Left";
                        break;
                    case DpadDirections.UpLeft:
                        result = "UpLeft";
                        break;

                    default:
                        break;
                }

                return result;
            }
        }

        public void UpdateState(Controller controller)
        {
            if (controller.IsConnected)
            {
                currentState = controller.GetState();
            }
            else
            {
                currentState = new State();
            }
        }
    }
}
