using System.Media;
using Timer = System.Windows.Forms.Timer;

namespace Gorillas
{
    public class Gorillas : Form
    {
        private const int SCREEN_WIDTH = 640;
        private const int SCREEN_HEIGHT = 350;
        private const double GRAVITY = 9.81;
        private const int BUILDING_MIN_HEIGHT = 100;
        private const int BUILDING_MAX_HEIGHT = 250;
        private const int NUM_BUILDINGS = 8;

        private readonly Random random = new Random();
        private List<Rectangle> buildings = new List<Rectangle>();
        private Point player1Position;
        private Point player2Position;
        private int player1Score = 0;
        private int player2Score = 0;
        private bool isPlayer1Turn = true;
        private List<Point> bananaTrajectory = new List<Point>();
        private double currentAngle = 45;
        private double currentVelocity = 50;
        private double windSpeed = 0;
        private bool isAiming = false;
        private bool isGameStarted = false;
        private SoundPlayer throwSound;
        private SoundPlayer explosionSound;
        private List<Point> explosionParticles = new List<Point>();

        public Gorillas()
        {
            this.ClientSize = new Size(SCREEN_WIDTH, SCREEN_HEIGHT);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Text = "Gorillas Game";
            InitializeSounds();
            ShowMainMenu();
        }

        private void InitializeSounds()
        {
            try
            {
                throwSound = new SoundPlayer("throw.wav");
                explosionSound = new SoundPlayer("explosion.wav");
            }
            catch (Exception)
            {
                // Handle missing sound files gracefully
            }
        }

        private void ShowMainMenu()
        {
            Controls.Clear();
            Button startButton = new Button
            {
                Text = "Start Game",
                Location = new Point(SCREEN_WIDTH / 2 - 50, SCREEN_HEIGHT / 2 - 25),
                Size = new Size(100, 50)
            };
            startButton.Click += (s, e) => StartGame();
            Controls.Add(startButton);
        }
        
        private void StartGame()
        {
            Controls.Clear();
            isGameStarted = true;
            GenerateBuildings();
            PlaceGorillas();
            GenerateWind();
            this.Focus();
            Invalidate();
        }

        private void ThrowBanana(double angle, double velocity)
        {
            // Clear previous trajectory
            bananaTrajectory.Clear();

            // Convert angle to radians and calculate initial velocities
            double radians = angle * Math.PI / 180.0;
            double vx = velocity * Math.Cos(radians);
            double vy = velocity * Math.Sin(radians);

            // Add wind effect
            vx += windSpeed * 0.1; // Wind affects horizontal velocity

            // Get starting position based on current player
            Point currentPos = isPlayer1Turn ? player1Position : player2Position;
            double x = currentPos.X;
            double y = currentPos.Y;

            // Time step for simulation
            const double dt = 0.1;

            // Play throw sound
            throwSound?.Play();

            // Disable input during throw
            isAiming = true;

            // Simulate banana trajectory
            while (y < SCREEN_HEIGHT && x >= 0 && x <= SCREEN_WIDTH)
            {
                // Add current position to trajectory
                bananaTrajectory.Add(new Point((int)x, (int)y));

                // Update position using physics equations
                x += vx * dt;
                vy += GRAVITY * dt;
                y += vy * dt;

                // Check for collision
                Point currentPoint = new Point((int)x, (int)y);
                if (CheckCollision(currentPoint))
                {
                    CreateExplosion(currentPoint);
                    HandleHit(currentPoint);
                    break;
                }

                // Force redraw to show animation
                Invalidate();
                Application.DoEvents();
                System.Threading.Thread.Sleep(20); // Control animation speed
            }

            // Re-enable input after throw
            isAiming = false;

            // Switch turns if no hit occurred
            if (bananaTrajectory.Count > 0 && !explosionParticles.Any())
            {
                isPlayer1Turn = !isPlayer1Turn;
                HandleUserInput();
            }
        }

        private void HandleHit(Point hitPosition)
        {
            // Define hit boxes for both gorillas
            const int GORILLA_SIZE = 20;
            Rectangle player1Rect = new Rectangle(
                player1Position.X - GORILLA_SIZE / 2,
                player1Position.Y - GORILLA_SIZE,
                GORILLA_SIZE,
                GORILLA_SIZE
            );

            Rectangle player2Rect = new Rectangle(
                player2Position.X - GORILLA_SIZE / 2,
                player2Position.Y - GORILLA_SIZE,
                GORILLA_SIZE,
                GORILLA_SIZE
            );

            // Determine which player was hit
            bool player1Hit = player1Rect.Contains(hitPosition);
            bool player2Hit = player2Rect.Contains(hitPosition);

            if (player1Hit || player2Hit)
            {
                // Update scores
                if (player1Hit)
                {
                    player2Score++;
                    MessageBox.Show($"Player 2 wins this round!\nScore: Player 1: {player1Score} - Player 2: {player2Score}");
                }
                else
                {
                    player1Score++;
                    MessageBox.Show($"Player 1 wins this round!\nScore: Player 1: {player1Score} - Player 2: {player2Score}");
                }

                // Check for game win
                if (player1Score >= 3 || player2Score >= 3)
                {
                    string winner = player1Score > player2Score ? "Player 1" : "Player 2";
                    if (MessageBox.Show($"{winner} wins the game!\nPlay again?", "Game Over",
                        MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        // Reset game
                        player1Score = 0;
                        player2Score = 0;
                        StartGame();
                    }
                    else
                    {
                        ShowMainMenu();
                    }
                }
                else
                {
                    // Start new round
                    ResetRound();
                }
            }
            else
            {
                // Building was hit, continue game
                isPlayer1Turn = !isPlayer1Turn;
                HandleUserInput();
            }
        }

        private void ResetRound()
        {
            // Clear existing game state
            buildings.Clear();
            bananaTrajectory.Clear();
            explosionParticles.Clear();

            // Generate new buildings
            GenerateBuildings();

            // Place gorillas in new positions
            PlaceGorillas();

            // Generate new wind conditions
            windSpeed = random.NextDouble() * 20 - 10; // Wind speed between -10 and 10

            // Reset game state flags
            isAiming = false;

            // Clear any existing UI elements
            foreach (Control control in Controls)
            {
                if (control != null)
                {
                    control.Dispose();
                }
            }
            Controls.Clear();

            // Redraw the game
            Invalidate();

            // Start new round with appropriate player
            HandleUserInput();
        }

        private bool CheckCollision(Point position)
        {
            // First check if we hit any buildings
            foreach (var building in buildings)
            {
                // Use Rectangle.Contains for precise collision detection
                if (building.Contains(position))
                {
                    return true;
                }
            }

            // Define collision rectangles for gorillas
            const int GORILLA_SIZE = 20;
            Rectangle player1Rect = new Rectangle(
                player1Position.X - GORILLA_SIZE / 2,
                player1Position.Y - GORILLA_SIZE,
                GORILLA_SIZE,
                GORILLA_SIZE
            );

            Rectangle player2Rect = new Rectangle(
                player2Position.X - GORILLA_SIZE / 2,
                player2Position.Y - GORILLA_SIZE,
                GORILLA_SIZE,
                GORILLA_SIZE
            );

            // Check for collision with either gorilla
            if (player1Rect.Contains(position) || player2Rect.Contains(position))
            {
                // Play explosion sound
                explosionSound?.Play();
                return true;
            }

            // No collision detected
            return false;
        }

        private void GenerateWind()
        {
            windSpeed = random.NextDouble() * 20 - 10; // Wind speed between -10 and 10
        }

        private void PlaceGorillas()
        {
            // Calculate positions for gorillas on first and last buildings
            Rectangle firstBuilding = buildings[0];
            Rectangle lastBuilding = buildings[NUM_BUILDINGS - 1];

            // Size of gorilla sprite
            const int GORILLA_WIDTH = 20;
            const int GORILLA_HEIGHT = 20;

            // Place first gorilla (Player 1)
            player1Position = new Point(
                firstBuilding.X + (firstBuilding.Width - GORILLA_WIDTH) / 2,
                firstBuilding.Y - GORILLA_HEIGHT
            );

            // Place second gorilla (Player 2)
            player2Position = new Point(
                lastBuilding.X + (lastBuilding.Width - GORILLA_WIDTH) / 2,
                lastBuilding.Y - GORILLA_HEIGHT
            );

            // Ensure gorillas are within screen bounds
            player1Position.X = Math.Max(GORILLA_WIDTH / 2,
                Math.Min(player1Position.X, SCREEN_WIDTH - GORILLA_WIDTH / 2));
            player2Position.X = Math.Max(GORILLA_WIDTH / 2,
                Math.Min(player2Position.X, SCREEN_WIDTH - GORILLA_WIDTH / 2));

            // Ensure gorillas are on top of buildings
            player1Position.Y = Math.Min(player1Position.Y,
                SCREEN_HEIGHT - GORILLA_HEIGHT);
            player2Position.Y = Math.Min(player2Position.Y,
                SCREEN_HEIGHT - GORILLA_HEIGHT);
        }

        private void GenerateBuildings()
        {
            buildings.Clear();
            int buildingWidth = SCREEN_WIDTH / NUM_BUILDINGS;
            int currentX = 0;

            // Generate random heights for each building
            for (int i = 0; i < NUM_BUILDINGS; i++)
            {
                // Vary the building heights while maintaining playability
                int minHeight = BUILDING_MIN_HEIGHT;
                int maxHeight = BUILDING_MAX_HEIGHT;

                // Add some variation to middle buildings
                if (i > 0 && i < NUM_BUILDINGS - 1)
                {
                    minHeight = Math.Max(BUILDING_MIN_HEIGHT,
                        buildings[i - 1].Height - 50);
                    maxHeight = Math.Min(BUILDING_MAX_HEIGHT,
                        buildings[i - 1].Height + 50);
                }

                // Generate building height
                int height = random.Next(minHeight, maxHeight);

                // Add some width variation
                int actualWidth = buildingWidth - random.Next(0, 10);

                // Create building rectangle
                Rectangle building = new Rectangle(
                    currentX,
                    SCREEN_HEIGHT - height,
                    actualWidth,
                    height
                );

                buildings.Add(building);
                currentX += buildingWidth;
            }

            // Ensure first and last buildings are suitable for gorillas
            buildings[0] = new Rectangle(
                buildings[0].X,
                SCREEN_HEIGHT - BUILDING_MAX_HEIGHT + 50,
                buildings[0].Width,
                BUILDING_MAX_HEIGHT - 50
            );

            buildings[NUM_BUILDINGS - 1] = new Rectangle(
                buildings[NUM_BUILDINGS - 1].X,
                SCREEN_HEIGHT - BUILDING_MAX_HEIGHT + 50,
                buildings[NUM_BUILDINGS - 1].Width,
                BUILDING_MAX_HEIGHT - 50
            );
        }

        private void HandleUserInput()
        {
            Label inputLabel = new Label
            {
                Text = $"Player {(isPlayer1Turn ? "1" : "2")} - Enter Angle (0-90) and Velocity (0-100):",
                Location = new Point(10, SCREEN_HEIGHT - 60),
                Size = new Size(300, 20)
            };

            TextBox angleBox = new TextBox
            {
                Location = new Point(10, SCREEN_HEIGHT - 40),
                Size = new Size(50, 20)
            };

            TextBox velocityBox = new TextBox
            {
                Location = new Point(70, SCREEN_HEIGHT - 40),
                Size = new Size(50, 20)
            };

            Button throwButton = new Button
            {
                Text = "Throw!",
                Location = new Point(130, SCREEN_HEIGHT - 40),
                Size = new Size(60, 20)
            };

            throwButton.Click += (s, e) =>
            {
                if (double.TryParse(angleBox.Text, out double angle) &&
                    double.TryParse(velocityBox.Text, out double velocity))
                {
                    if (angle >= 0 && angle <= 90 && velocity >= 0 && velocity <= 100)
                    {
                        Controls.Clear();
                        ThrowBanana(angle, velocity);
                    }
                }
            };

            Controls.Add(inputLabel);
            Controls.Add(angleBox);
            Controls.Add(velocityBox);
            Controls.Add(throwButton);
        }

        private void CreateExplosion(Point center)
        {
            explosionParticles.Clear();
            for (int i = 0; i < 20; i++)
            {
                double angle = random.NextDouble() * Math.PI * 2;
                double distance = random.NextDouble() * 20;
                int x = center.X + (int)(Math.Cos(angle) * distance);
                int y = center.Y + (int)(Math.Sin(angle) * distance);
                explosionParticles.Add(new Point(x, y));
            }

            explosionSound?.Play();
            Timer explosionTimer = new Timer { Interval = 1000 };
            explosionTimer.Tick += (s, e) =>
            {
                explosionParticles.Clear();
                ((Timer)s).Stop();
                HandleUserInput();
            };
            explosionTimer.Start();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // Fill background
            g.FillRectangle(Brushes.DarkBlue, 0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);

            // Draw buildings
            using (SolidBrush buildingBrush = new SolidBrush(Color.Gray))
            {
                foreach (var building in buildings)
                {
                    g.FillRectangle(buildingBrush, building);
                }
            }

            // Draw gorillas
            using (SolidBrush gorillaBrush = new SolidBrush(Color.Brown))
            {
                g.FillEllipse(gorillaBrush, player1Position.X - 10, player1Position.Y - 20, 20, 20);
                g.FillEllipse(gorillaBrush, player2Position.X - 10, player2Position.Y - 20, 20, 20);
            }

            // Draw banana trajectory
            if (bananaTrajectory.Count > 0)
            {
                using (Pen trajectoryPen = new Pen(Color.Yellow, 2))
                {
                    for (int i = 1; i < bananaTrajectory.Count; i++)
                    {
                        g.DrawLine(trajectoryPen,
                            bananaTrajectory[i - 1],
                            bananaTrajectory[i]);
                    }
                }
            }

            // Draw explosion particles
            if (explosionParticles.Count > 0)
            {
                using (SolidBrush explosionBrush = new SolidBrush(Color.Red))
                {
                    foreach (var particle in explosionParticles)
                    {
                        g.FillEllipse(explosionBrush, particle.X - 2, particle.Y - 2, 4, 4);
                    }
                }
            }

            // Draw wind indicator
            string windIndicator = $"Wind: {(windSpeed > 0 ? "→" : "←")} {Math.Abs(windSpeed):F1}";
            g.DrawString(windIndicator, new Font("Arial", 10), Brushes.White, new Point(SCREEN_WIDTH / 2 - 40, 10));

            // Draw scores
            using (Font scoreFont = new Font("Arial", 12))
            {
                g.DrawString($"Player 1: {player1Score}", scoreFont, Brushes.White, 10, 10);
                g.DrawString($"Player 2: {player2Score}", scoreFont, Brushes.White, SCREEN_WIDTH - 100, 10);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                ShowMainMenu();
            }
            else if (isGameStarted && !isAiming)
            {
                HandleUserInput();
            }
        }
    }
}
