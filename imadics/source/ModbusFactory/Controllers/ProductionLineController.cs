using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SoftControllers;
using FTRIG = SoftControllers.FallingEdgeDetector; 
using RTRIG = SoftControllers.RaisingEdgeDetector;
using TON = SoftControllers.OnDelayTimer; 

namespace SampleScenes.Controllers
{
    public class ProductionLineController : ModbusController
    {

        Coil lidsRawConveyor; 
        Coil lidsCenterStart;
        HoldingRegister lidsCounter;
        Coil lidsExitConveyor1;
        Coil lidsExitConveyor2;

        DiscreteInput lidsAtEntry;
        DiscreteInput lidsCenterBusy;

        Coil basesRawConveyor;
        Coil basesCenterStart;
        HoldingRegister basesCounter;
        Coil basesExitConveyor1;
        Coil basesExitConveyor2;

        Coil exitConveyor;

        DiscreteInput basesAtEntry;
        DiscreteInput basesCenterBusy;
        FTRIG ftLidsAtEntry = new FTRIG();
        FTRIG ftLidsCenterBusy = new FTRIG();

        FTRIG ftBasesAtEntry = new FTRIG();
        FTRIG ftBasesCenterBusy = new FTRIG();

        bool feedLidMaterial;
        bool feedBaseMaterial;

        Screen _screen;

        public ProductionLineController(ConnectionInformation connection, RegisterMap registerMap, Screen screen) 
        : base(connection.endPoint, connection.deviceId, new StreamWriter($"Log_{nameof(ProductionLineController)}.csv"))
        {
            this._screen = screen;
            this.CycleTime = 50;

             lidsRawConveyor = registerMap.GetCoil(Device,"Lids raw conveyor");
             lidsCenterStart = registerMap.GetCoil(Device,"Lids center (start)");
             lidsCounter = registerMap.GetHoldingRegister(Device,"Lids counter");
             lidsExitConveyor1 = registerMap.GetCoil(Device,"Lids exit conveyor 1");
             lidsExitConveyor2 = registerMap.GetCoil(Device,"Lids exit conveyor 2");

             lidsAtEntry = registerMap.GetDiscreteInput(Device,"Lids at entry");
             lidsCenterBusy = registerMap.GetDiscreteInput(Device,"Lids center (busy)");
             //lidsCenterRunning = registerMap.GetDiscreteInput(Device,"Lids center (running)");

             basesRawConveyor = registerMap.GetCoil(Device,"Bases raw conveyor");
             basesCenterStart = registerMap.GetCoil(Device,"Bases center (start)");
             basesCounter = registerMap.GetHoldingRegister(Device,"Bases counter");
             basesExitConveyor1 = registerMap.GetCoil(Device,"Bases exit conveyor 1");
             basesExitConveyor2 = registerMap.GetCoil(Device,"Bases exit conveyor 2");

             exitConveyor = registerMap.GetCoil(Device,"Exit conveyor");

             basesAtEntry = registerMap.GetDiscreteInput(Device,"Bases at entry");
             basesCenterBusy = registerMap.GetDiscreteInput(Device,"Bases center (busy)");
             //basesCenterRunning = registerMap.GetDiscreteInput(Device,"Bases center (running)");
            
            AutoRegisterDatapoints(this);
        }

        protected override Task FinalizeAsync()
        {
            return Task.CompletedTask;
        }

        protected override Task InitializeAsync(CancellationToken cancel)
        {
            lidsRawConveyor.Value = false;
            lidsCenterStart.Value = true;

            basesRawConveyor.Value = false;
            basesCenterStart.Value = true;

            feedLidMaterial = true;
            feedBaseMaterial = true;

            lidsCounter.Value = 0;
            basesCounter.Value = 0;

            lidsExitConveyor1.Value = true;
            lidsExitConveyor2.Value = true;
            basesExitConveyor1.Value = true;
            basesExitConveyor2.Value = true;
            exitConveyor.Value = true;

            _screen.MoveAddString (5, 2, $"=== CONTROLLER ===");
            _screen.MoveAddString (6, 2, $"Production Line, connected to {Device.EndPoint}/{Device.DeviceId}.");
            _screen.Refresh ();
            return Task.CompletedTask;
        }
        protected override Task ExecuteAsync(int cycle, int elapsedMilliseconds, CancellationToken cancel)
        {
             //Lids
            ftLidsAtEntry.CLK(!lidsAtEntry.Value);
            ftLidsCenterBusy.CLK(lidsCenterBusy.Value);

            if (ftLidsAtEntry.Q)
            {
                feedLidMaterial = false;
            }

            if (ftLidsCenterBusy.Q)
            {
                feedLidMaterial = true;
                lidsCounter.Value++;
            }

            lidsRawConveyor.Value = feedLidMaterial && !lidsCenterBusy.Value;

            //Bases
            ftBasesAtEntry.CLK(!basesAtEntry.Value);
            ftBasesCenterBusy.CLK(basesCenterBusy.Value);

            if (ftBasesAtEntry.Q)
            {
                feedBaseMaterial = false;
            }

            if (ftBasesCenterBusy.Q)
            {
                feedBaseMaterial = true;
                basesCounter.Value++;
            }

            basesRawConveyor.Value = feedBaseMaterial && !basesCenterBusy.Value;


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
            _screen.MoveAddString (11, 2, $"Feed Lid Material = {feedLidMaterial.ToString()}   ");
            _screen.MoveAddString (12, 2, $"Feed BAse Material = {feedBaseMaterial.ToString()}   ");


            _screen.MoveAddString (10, 32, $"== SENSORS ==");
            _screen.MoveAddString (11, 32, $"{lidsAtEntry.ToString()}    ");
            _screen.MoveAddString (12, 32, $"{lidsCenterBusy.ToString()}    ");
            //_screen.MoveAddString (13, 32, $"{lidsCenterRunning.ToString()}    ");

            _screen.MoveAddString (15, 32, $"{basesAtEntry.ToString()}    ");
            _screen.MoveAddString (16, 32, $"{basesCenterBusy.ToString()}    ");
            //_screen.MoveAddString (17, 32, $"{basesCenterRunning.ToString()}    ");


            _screen.MoveAddString (10, 82, $"== ACTUATORS ==");
            _screen.MoveAddString (12, 82, $"{lidsRawConveyor.ToString()}    ");
            _screen.MoveAddString (13, 82, $"{lidsCenterStart.ToString()}    ");
            _screen.MoveAddString (14, 82, $"{lidsCounter.ToString()}    ");
            _screen.MoveAddString (15, 82, $"{lidsExitConveyor1.ToString()}    ");
            _screen.MoveAddString (16, 82, $"{lidsExitConveyor2.ToString()}    ");

            _screen.MoveAddString (18, 82, $"{basesRawConveyor.ToString()}    ");
            _screen.MoveAddString (19, 82, $"{basesCenterStart.ToString()}    ");
            _screen.MoveAddString (20, 82, $"{basesCounter.ToString()}    ");
            _screen.MoveAddString (21, 82, $"{basesExitConveyor1.ToString()}    ");
            _screen.MoveAddString (22, 82, $"{basesExitConveyor2.ToString()}    ");

            _screen.MoveAddString (24, 82, $"{exitConveyor.ToString()}    ");
       
            _screen.Refresh ();
        }
        
    }
}