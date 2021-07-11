using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class ElmuTaxi : PhysicsGame
{
    /// <summary>
    /// Game state
    /// </summary>
    public bool GameRunning = true;

    /// <summary>
    /// Our player's taxi
    /// </summary>
    public PhysicsObject Player;

    /// <summary>
    /// List of all NPC cars
    /// </summary>
    public List<PhysicsObject> Cars = new List<PhysicsObject>();

    /// <summary>
    /// List of all road segments
    /// </summary>
    public List<GameObject> Roads = new List<GameObject>();

    /// <summary>
    /// List of all fuel pickups
    /// </summary>
    public List<PhysicsObject> FuelCans = new List<PhysicsObject>();

    //Constants
    const double CAR_SPAWNRATE_MIN = 1.1;
    const double CAR_SPAWNRATE_LIMIT = 1;
    const double CAR_SPAWNRATE_MAX = 2.2;
    const double FUEL_SPAWNRATE_MIN = 7.5;
    const double FUEL_SPAWNRATE_MAX = 16;
    const double CAR_ADJACENT_LANE_COOLDOWN = 2.3;
    const double PLR_MAX_ACCELERATION = 5;
    const double PLR_ACCELERATION = 150;
    const double PLR_DECCELERATION = 75;
    const double CAR_NPC_SPEED = 350;
    const double CAR_MAXSPEED_DEFAULT = 950.0;

    Image RoadTexture = LoadImage("road");
    Image FuelCanTex = LoadImage("jerrycan");
    DoubleMeter FuelGauge;
    IntMeter DistanceCounter;
    IntMeter Speedometer;
    Label GameOverDisplay;
    bool NoFuel = false;
    double DistanceTravelled = 0.0;
    double NextFuelDrop = 5.0;
    double FuelUsePerSecond = 0.25;
    double CurrentCarSpeed = 1;
    double CurrentCarMaxSpeed = 950.0;

    double TotalCooldown = 1;
    List<double> LaneXPositions = new List<double>() {-340, -140, 60, 280};
    List<double> LaneXCooldown = new List<double>() { 8, 2, 8, 8 };
    List<double> LaneXPrevSpawn = new List<double>() { 0, 0, 0, 0 };
    List<Image> CarTextures = new List<Image>()
    {
        LoadImage("taxi"),
        LoadImage("Ambulance"),
        LoadImage("Audi"),
        LoadImage("Black_viper"),
        LoadImage("Car"),
        LoadImage("Mini_truck"),
        LoadImage("Mini_van"),
        LoadImage("Police"),
        LoadImage("truck")
    };

    /// <summary>
    /// Spawn a new car on a lane
    /// </summary>
    /// <param name="lane">lane to spawn or 0 by default</param>
    private void SpawnCar(int lane = 0)
    {
        //set lane on CD
        LaneXCooldown[lane] = Utils.Math.RandomDouble(CAR_SPAWNRATE_MIN, CAR_SPAWNRATE_MAX);
        TotalCooldown = CAR_SPAWNRATE_LIMIT;
        Random rnd = new Random();
        //choose texture
        Image carTex = CarTextures[rnd.Next(0, CarTextures.Count - 1)];
        PhysicsObject newCar = new PhysicsObject(carTex);
        //choose lane
        newCar.Position = Level.Center + new Vector(LaneXPositions[lane], 1300);
        Cars.Add(newCar);
        Add(newCar, 2);
        Debug.WriteLine("Spawned a car!");
    }

    /// <summary>
    /// Spawns a fuel pickup on a lane
    /// </summary>
    /// <param name="lane"></param>
    private void SpawnFuel(int lane = 0)
    {
        //set lane on CD
        LaneXCooldown[lane] = Utils.Math.RandomDouble(CAR_SPAWNRATE_MIN, CAR_SPAWNRATE_MAX);
        TotalCooldown = CAR_SPAWNRATE_LIMIT;
        Random rnd = new Random();
        PhysicsObject fuelPickup = new PhysicsObject(FuelCanTex);
        fuelPickup.Tag = "Fuel";
        //choose lane
        fuelPickup.Position = Level.Center + new Vector(LaneXPositions[lane], 1300);
        FuelCans.Add(fuelPickup);
        Add(fuelPickup, 2);
        Debug.WriteLine("Spawned a fuel can!");
    }

    /// <summary>
    /// Returns if a lane hasnt spawned a car in CAR_ADJACENT_LANE_COOLDOWN
    /// </summary>
    /// <param name="lane"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    bool LaneHasntSpawnedIn(int lane, double time)
    {
        return (time - LaneXPrevSpawn[lane]) > CAR_ADJACENT_LANE_COOLDOWN;
    }

    /// <summary>
    /// Returns true if one of lanes adjacent havent spawned anything in CAR_ADJACENT_LANE_COOLDOWN
    /// </summary>
    /// <param name="lane"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    bool LaneCanSpawn(int lane, double time)
    {
        switch (lane)
        {
            case 0:
                return (LaneHasntSpawnedIn(1, time) || LaneHasntSpawnedIn(2, time) || LaneHasntSpawnedIn(3, time));
            case 1:
                return (LaneHasntSpawnedIn(0, time) || LaneHasntSpawnedIn(2, time) || LaneHasntSpawnedIn(3, time));
            case 2:
                return (LaneHasntSpawnedIn(1, time) || LaneHasntSpawnedIn(0, time) || LaneHasntSpawnedIn(3, time));
            case 3:
                return (LaneHasntSpawnedIn(1, time) || LaneHasntSpawnedIn(2, time) || LaneHasntSpawnedIn(0, time));
            default:
                return false;
        }
    }

    /// <summary>
    /// Print lane CD for debugging Lane spawn rules
    /// </summary>
    void PrintLaneCD()
    {
        Debug.WriteLine("Lane X CD = (" + LaneXCooldown[0].ToString() + ", " + LaneXCooldown[1].ToString() + ", " + LaneXCooldown[2].ToString() + ", " + LaneXCooldown[3].ToString() + ")");
    }

    protected override void Update(Time time)
    {
        base.Update(time);
        double total = time.SinceStartOfGame.TotalSeconds;
        double dt = time.SinceLastUpdate.TotalSeconds;
        TotalCooldown--;

        Speedometer.Value = (int)(CurrentCarSpeed / 5.5);
        DistanceCounter.Value = (int)(DistanceTravelled / 5.5);

        if (GameRunning && CurrentCarSpeed > 0)
            DistanceTravelled += CurrentCarSpeed * dt;

        //game over if we run out of speed
        if (GameRunning && CurrentCarSpeed <= 0)
        {
            Debug.WriteLine("Car stopped, game over!");
            GameOver("Game over. You ran out of fuel!");
            GameRunning = false;
        }

        //Update fuel
        if (GameRunning)
        {
            FuelGauge.Value -= FuelUsePerSecond * dt;
            CurrentCarMaxSpeed += PLR_MAX_ACCELERATION * dt;
        }

        if (!NoFuel)
        {
            //we have fuel
            CurrentCarSpeed = Math.Clamp(CurrentCarSpeed + PLR_ACCELERATION * dt, 0, CurrentCarMaxSpeed); 
        }
        else
        {
            //we dont have fuel
            CurrentCarSpeed = Math.Clamp(CurrentCarSpeed - PLR_DECCELERATION * dt, 0, CurrentCarMaxSpeed);
        }

        //Handle car spawning on lanes
        if (GameRunning)
        {
            for (int lane = 0; lane < LaneXCooldown.Count; lane++)
            {
                double cd = LaneXCooldown[lane];
                if (cd < 0 && TotalCooldown < 0 && LaneCanSpawn(lane, total)) //TODO: investigate if total CD is still necessary after introduction of LaneCanSpawn ?
                {
                    if (total > NextFuelDrop)
                    {
                        NextFuelDrop = total + Utils.Math.RandomDouble(FUEL_SPAWNRATE_MIN, FUEL_SPAWNRATE_MAX);
                        SpawnFuel(lane);
                        LaneXPrevSpawn[lane] = total;
                    }
                    else
                    {
                        SpawnCar(lane);
                        LaneXPrevSpawn[lane] = total;
                    }
                }
                LaneXCooldown[lane] -= dt;
            }
        }

        //remove cars out of view
        var carsRemaining = Cars.Where(c => {
            if (c.Position.Y < Level.Center.Y - 1200.0)
            {
                c.Destroy();
                return false;
            }
            else
            {
                return true;
            }
        });
        //update remaining cars
        foreach (PhysicsObject car in carsRemaining)
        {
            if (GameRunning)
                car.Position -= new Vector(0, (CurrentCarSpeed - CAR_NPC_SPEED) * dt);
        }
        Cars = carsRemaining.ToList();
        //remove fuels out of view
        var fuelsRemaining = FuelCans.Where(c => {
            if ((c.Position.Y < Level.Center.Y - 1200.0) || !c.IsUpdated)
            {
                c.Destroy();
                return false;
            }
            else
            {
                return true;
            }
        });
        //update remaining fuels
        foreach (PhysicsObject fuel in fuelsRemaining)
        {
            if (GameRunning && fuel != null && fuel.IsUpdated)
                fuel.Position -= new Vector(0.0, CurrentCarSpeed * dt);
        }
        FuelCans = fuelsRemaining.ToList();
        //update roads
        foreach (GameObject road in Roads)
        {
            if (GameRunning)
            {
                road.Position -= new Vector(0, CurrentCarSpeed * dt);
                if (road.Position.Y < Level.Center.Y - 2000.0)
                {
                    road.Position = new Vector(0, 2000);
                }
            }
        }

        //if (Player != null)
            //Debug.WriteLine("player pos: " + Player.Position.ToString());

        //Player.Position = Mouse.PositionOnWorld;
    }

    /// <summary>
    /// Move player's taxi towards vector (clamped to road)
    /// </summary>
    /// <param name="dir"></param>
    private void MovePlayer(Vector dir)
    {
        if (GameRunning)
        {
            Vector pos = Player.Position;
            Vector newPos = new Vector(
                Math.Clamp(pos.X + dir.X, -400.0, 340),
                pos.Y + dir.Y
            );
            Player.Position = newPos;
        }
    }

    /// <summary>
    /// Set up all keyboard/mouse/gamepad listeners
    /// </summary>
    private void ConnectListeners()
    {
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.R, ButtonState.Pressed, ResetGame, "Aloita uudestaan");
        Keyboard.Listen(Key.A, ButtonState.Down, MovePlayer, null, new Vector(-25, 0));
        Keyboard.Listen(Key.D, ButtonState.Down, MovePlayer, null, new Vector(25, 0));
    }
    
    /// <summary>
    /// Handler for Car vs. Car collisions
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public void PlayerCollision(PhysicsObject a, PhysicsObject b) //TODO:  Rename me?
    {
        bool isFuel = FuelCans.Contains(b);
        bool isCar = Cars.Contains(b);

        if (isCar)
        {
            GameOver("Game over. You hit a car!");
            Debug.WriteLine("Car hit an npc car, GAME OVER!");
        }
        else if (isFuel && a == Player)
        {
            Debug.WriteLine("PICK UP FUEL!!!!!!!");
            FuelGauge.Value = 100;
            b.IsUpdated = false;
        }
    }

    public void GameOver(string reason = "You Lost!")
    {
        GameRunning = false;
        CurrentCarMaxSpeed = CAR_MAXSPEED_DEFAULT;
        GameOverDisplay = new Label();
        GameOverDisplay.HorizontalAlignment = HorizontalAlignment.Center;
        GameOverDisplay.Text = String.Format("{0}\nDistance Travelled:{1}\nPress 'R' to restart", reason, Math.Round(DistanceTravelled / 5.5, 2).ToString());
        //display.Font = LoadFont("diablo_h");
        GameOverDisplay.Position = new Vector(25.0, 50.0);
        GameOverDisplay.TextScale = new Vector(2, 2);
        GameOverDisplay.TextColor = Color.Red;
        Add(GameOverDisplay);
        //Player.Animation = null;
        //Player.Image = LoadImage("headstone");
        //Player.Angle = Angle.Zero;
    }

    /// <summary>
    /// Handle resetting the game state after game over, or on request
    /// </summary>
    private void ResetGame()
    {
        Debug.WriteLine("Resetting game");
        DistanceTravelled = 0;
        CurrentCarSpeed = 1;
        DistanceCounter.Value = 0;
        FuelGauge.Value = 100;
        NoFuel = false;
        GameRunning = true;
        Player.Position = Level.Center - new Vector(0, 750);
        foreach (PhysicsObject car in Cars)
            car.Destroy();

        foreach (PhysicsObject fuel in FuelCans)
            fuel.Destroy();

        foreach (GameObject r in Roads)
            r.Destroy();

        Cars = new List<PhysicsObject>();
        FuelCans = new List<PhysicsObject>();
        Roads = new List<GameObject>();

        if (GameOverDisplay != null)
            GameOverDisplay.Destroy();


        LaneXCooldown = new List<double>() { 5, 0, 5, 5 };
        LaneXPrevSpawn = new List<double>() { 0, 0, 0, 0 };

        AddRoad(Vector.Zero);
        AddRoad(new Vector(0, 1950));
        AddRoad(new Vector(0, -1950));
    }

    /// <summary>
    /// Add a new road segment
    /// </summary>
    /// <param name="pos"></param>
    private void AddRoad(Vector pos)
    {
        GameObject newRoad = new GameObject(RoadTexture);
        newRoad.Position = pos;
        Debug.WriteLine("road size: " + newRoad.Size.ToString());
        Roads.Add(newRoad);
        Add(newRoad, 1);
    }


    void FuelEmpty()
    {
        NoFuel = true;
    }


    public override void Begin()
    {
        Level.BackgroundColor = Color.DarkGreen;
        Player = new PhysicsObject(LoadImage("taxi"));
        Player.MakeStatic();
        Player.Position = Level.Center - new Vector(0, 750);
        Level.Size = new Vector(1400, 2000);
        //Player.Image = LoadImage("taxi");
        Camera.ZoomToLevel();
        Player.Shape = Shape.Rectangle;

        //taxi collision handler hook
        AddCollisionHandler(Player, PlayerCollision); //TODO: maybe move to ConnectListeners?

        Add(Player, 3);
        ConnectListeners();

        AddRoad(Vector.Zero);
        AddRoad(new Vector(0, 1950));
        AddRoad(new Vector(0, -1950));

        //create Fuel gauge
        FuelGauge = new DoubleMeter(100);
        FuelGauge.MaxValue = 10;
        FuelGauge.LowerLimit += FuelEmpty;
        ProgressBar fuelBar = new ProgressBar(150, 20);
        fuelBar.X = Screen.Left + 150;
        fuelBar.Y = Screen.Top - 20;
        fuelBar.BarColor = Color.Yellow;
        fuelBar.BindTo(FuelGauge);
        Add(fuelBar);
        //fuel icon next to gauge.  TODO: does it have to be gameobject? Aren't there UI elements??? investigate pls
        GameObject fuel = new GameObject(FuelCanTex);
        fuel.Position = new Vector(Screen.Left - 675, Screen.Top + 570);
        Add(fuel, 3);

        // Add speed label and counter
        //Label speedLabel = new Label();
        //speedLabel.Text = "Speed:";
        //speedLabel.TextColor = Color.White;
        //speedLabel.X = Screen.Left + 80;
        //speedLabel.Y = Screen.Top - 100;
        //Add(speedLabel);

        Speedometer = new IntMeter(0);
        Label speedoMeter = new Label();
        speedoMeter.Title = "Speed: ";
        speedoMeter.X = Screen.Left + 90;
        speedoMeter.Y = Screen.Top - 75;
        speedoMeter.TextColor = Color.White;
        speedoMeter.Color = Color.Transparent;
        speedoMeter.HorizontalAlignment = HorizontalAlignment.Left;
        speedoMeter.BindTo(Speedometer);
        Add(speedoMeter);

        //Add distance label and counter
        //Label distLabel = new Label();
        //distLabel.Text = "Distance:";
        //distLabel.TextColor = Color.White;
        //distLabel.X = Screen.Left + 67;
        //distLabel.Y = Screen.Top - 150;
        //Add(distLabel);
        
        DistanceCounter = new IntMeter(0);
        Label distanceDisplay = new Label();
        distanceDisplay.Title = "Distance: ";
        distanceDisplay.X = Screen.Left + 101;
        distanceDisplay.Y = Screen.Top - 95;
        distanceDisplay.TextColor = Color.White;
        distanceDisplay.Color = Color.Transparent;
        distanceDisplay.HorizontalAlignment = HorizontalAlignment.Left;

        distanceDisplay.BindTo(DistanceCounter);
        Add(distanceDisplay);
    }
}

