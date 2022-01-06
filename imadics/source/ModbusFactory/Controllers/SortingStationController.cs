using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mindmagma.Curses;
using Modbus.Device;
using System.Linq;
using SoftControllers;
using SampleScenes;
using System.IO;

namespace SampleScenes.Controllers
{

    /// <summary>
    /// Implements a MODBUS controller for Sorting Station Scene.
    /// <para>
    /// See https://docs.factoryio.com/manual/scenes/sorting-station/
    /// </para>
    /// </summary>
    public class SortingStationController : ModbusController 
    {
        Screen _screen;
        Coil entryConveyor;
        Coil sorter1Turn; 
        Coil sorter1Belt;
        Coil sorter2Turn; 
        Coil sorter2Belt; 
        Coil sorter3Turn; 
        Coil sorter3Belt; 
        Coil exitConveyor; 
        Coil stopBlade;
        InputRegister visionSensor; 
        DiscreteInput atExit; 
        FallingEdgeDetector ftAtExit = new FallingEdgeDetector();
        State feederState = State.State0;
        State sorterState = State.State0;

        public SortingStationController (ConnectionInformation connection, RegisterMap registerMap, Screen screen) 
        : base(connection.endPoint, connection.deviceId, new StreamWriter($"Log_{nameof(SortingStationController)}.csv"))
        {
            this._screen = screen;
            this.CycleTime = 50;

            entryConveyor   = registerMap.GetCoil(Device,"Entry conveyor"); 
            sorter1Turn     = registerMap.GetCoil(Device, "Sorter 1 turn");
            sorter1Belt     = registerMap.GetCoil(Device, "Sorter 1 belt");
            sorter2Turn     = registerMap.GetCoil(Device, "Sorter 2 turn"); 
            sorter2Belt     = registerMap.GetCoil(Device, "Sorter 2 belt"); 
            sorter3Turn     = registerMap.GetCoil(Device, "Sorter 3 turn"); 
            sorter3Belt     = registerMap.GetCoil(Device, "Sorter 3 belt"); 
            exitConveyor    = registerMap.GetCoil(Device, "Exit conveyor"); 
            stopBlade       = registerMap.GetCoil(Device, "Stop blade"); 
            visionSensor    = registerMap.GetInputRegister(Device, "Vision sensor");
            atExit          = registerMap.GetDiscreteInput(Device, "At exit");

            AutoRegisterDatapoints(this);
        }

        /// <summary>
        /// Performs necessary initialization steps of the factory.
        /// It sets the coils and let the run system dry for 10s.
        /// </summary>
        protected override Task InitializeAsync (CancellationToken token) {
            sorter1Turn.Value = false;
            sorter1Belt.Value = false;
            sorter2Turn.Value = false;
            sorter2Belt.Value = false;
            sorter3Turn.Value = false;
            sorter3Belt.Value = false;
            exitConveyor.Value = false;
            stopBlade.Value = false;
            entryConveyor.Value = true;

            _screen.MoveAddString (5, 2, $"=== CONTROLLER ===");
            _screen.MoveAddString (6, 2, $"Sorting Station Controller, connected to {Device.EndPoint}/{Device.DeviceId}.");
            _screen.Refresh ();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes single control actions. 
        /// <para>
        /// This code was adapted from: 
        /// https://github.com/realgamessoftware/factoryio-sdk/blob/master/samples/Controllers/Scenes/SortingStation.cs
        /// </para>
        /// </summary>
        protected override Task ExecuteAsync(int cycle, int elapsedMilliseconds, CancellationToken cancel)
        {
            ftAtExit.CLK(!atExit.Value);

            //Feeder
            if (feederState == State.State0)
            {
                if (visionSensor.Value != 0)
                {
                    if (sorterState == State.State0)
                    {
                        //Set sorter state
                        if (visionSensor.Value == 1)
                        {
                            //Blue raw material
                            sorterState = State.State1;
                        }
                        else if (visionSensor.Value == 4)
                        {
                            //Green raw material
                            sorterState = State.State2;
                        }
                        else
                        {
                            //Other
                            sorterState = State.State3;
                        }

                        feederState = State.State1;
                    }
                    else
                    {
                        entryConveyor.Value = false;
                        stopBlade.Value = true;
                    }
                }
            }
            else if (feederState == State.State1)
            {
                entryConveyor.Value = true;

                if (visionSensor.Value == 0)
                {
                    feederState = State.State0;
                }
            }

            //Sorters
            if (sorterState == State.State0)
            {
                sorter1Belt.Value = false;
                sorter1Turn.Value = false;
                sorter2Belt.Value = false;
                sorter2Turn.Value = false;
                sorter3Belt.Value = false;
                sorter3Turn.Value = false;
                exitConveyor.Value = false;
                
            }
            else if (sorterState == State.State1) //First sorter
            {
                exitConveyor.Value = true;

                sorter1Belt.Value = true;
                sorter1Turn.Value = true;
                sorter2Belt.Value = false;
                sorter2Turn.Value = false;
                sorter3Belt.Value = false;
                sorter3Turn.Value = false;

                if (ftAtExit.Q)
                {   
                    stopBlade.Value = false;
                    sorterState = State.State0;
                }
            }
            else if (sorterState == State.State2) //Second sorter
            {
                exitConveyor.Value = true;

                sorter1Belt.Value = false;
                sorter1Turn.Value = false;
                sorter2Belt.Value = true;
                sorter2Turn.Value = true;
                sorter3Belt.Value = false;
                sorter3Turn.Value = false;

                if (ftAtExit.Q)
                {
                    stopBlade.Value = false;
                    sorterState = State.State0;
                }
            }
            else if (sorterState == State.State3) //Third sorter
            {
                exitConveyor.Value = true;

                sorter1Belt.Value = false;
                sorter1Turn.Value = false;
                sorter2Belt.Value = false;
                sorter2Turn.Value = false;
                sorter3Belt.Value = true;
                sorter3Turn.Value = true;

                if (ftAtExit.Q)
                {
                    stopBlade.Value = false;
                    sorterState = State.State0;
                }
            }
            _ = Task.Run(() => UpdateUI(elapsedMilliseconds));
            return Task.CompletedTask;
        }

        ElapsedTimeObserver elapsedObserver = new ElapsedTimeObserver(50);
        private void UpdateUI (int elapsedMilliseconds) 
        {
            elapsedObserver.Add(elapsedMilliseconds);
            _screen.MoveAddString (7, 2, $"Running time = {elapsedObserver.RunningTime}");
            _screen.MoveAddString (8, 2, $"Cycle time: avg10 = {elapsedObserver.MovingAvg}, max = {elapsedObserver.Max}, min = {elapsedObserver.Min}");       

            _screen.MoveAddString (10, 2, $"== STATES ==");
            _screen.MoveAddString (11, 2, $"FeederState = {feederState.ToString()}   ");
            _screen.MoveAddString (12, 2, $"SorterState = {sorterState.ToString()}   ");


            _screen.MoveAddString (10, 32, $"== SENSORS ==");
            _screen.MoveAddString (11, 32, $"{atExit.ToString()}    ");
            _screen.MoveAddString (12, 32, $"{visionSensor.ToString()}    ");

            _screen.MoveAddString (10, 82, $"== ACTUATORS ==");
            _screen.MoveAddString (11, 82, $"{entryConveyor.ToString()}    ");
            _screen.MoveAddString (12, 82, $"{stopBlade.ToString()}    ");
            _screen.MoveAddString (13, 82, $"{exitConveyor.ToString()}    ");

            _screen.MoveAddString (15, 82, $"{sorter1Turn.ToString()}    ");
            _screen.MoveAddString (16, 82, $"{sorter1Belt.ToString()}    ");

            _screen.MoveAddString (18, 82, $"{sorter2Turn.ToString()}    ");
            _screen.MoveAddString (19, 82, $"{sorter2Belt.ToString()}    ");

            _screen.MoveAddString (21, 82, $"{sorter3Turn.ToString()}    ");
            _screen.MoveAddString (22, 82, $"{sorter3Belt.ToString()}    ");
            _screen.Refresh ();
        }

        protected override Task FinalizeAsync()
        {
            return Task.CompletedTask;
        }
    }

}
