using UnityEngine;

/// <summary>
/// PongGame - A simple Pong game implementation for Unity
/// This script manages the entire game: paddles, ball, scoring, and win condition
/// Attach this script to an empty GameObject in your scene
/// </summary>
public class PongGame : MonoBehaviour
{
    // ===========================================
    // GAME SETTINGS
    // ===========================================
    
    [Header("Game Area Settings")]
    [Tooltip("Width of the game area in world units")]
    public float gameWidth = 16f;
    
    [Tooltip("Height of the game area in world units")]
    public float gameHeight = 12f;
    
    [Header("Paddle Settings")]
    [Tooltip("Height of each paddle")]
    public float paddleHeight = 2f;
    
    [Tooltip("Width of each paddle")]
    public float paddleWidth = 0.3f;
    
    [Tooltip("How fast paddles move")]
    public float paddleSpeed = 8f;
    
    [Tooltip("Distance from the edge for paddle placement")]
    public float paddleOffset = 1f;
    
    [Header("Ball Settings")]
    [Tooltip("Size of the ball")]
    public float ballSize = 0.4f;
    
    [Tooltip("Initial speed of the ball")]
    public float ballSpeed = 6f;
    
    [Tooltip("Speed increase after each paddle hit")]
    public float ballSpeedIncrease = 0.2f;
    
    [Tooltip("Maximum ball speed")]
    public float maxBallSpeed = 15f;
    
    [Header("Game Rules")]
    [Tooltip("Points needed to win the game")]
    public int winningScore = 5;
    
    // ===========================================
    // PRIVATE VARIABLES
    // ===========================================
    
    // Game objects
    private GameObject leftPaddle;
    private GameObject rightPaddle;
    private GameObject ball;
    private GameObject topWall;
    private GameObject bottomWall;
    
    // Score display objects
    private GameObject player1ScoreDisplay;
    private GameObject player2ScoreDisplay;
    
    // Win screen UI
    private GameObject winScreen;
    
    // Ball movement
    private Vector2 ballVelocity;
    private Vector2 pendingBallDirection; // Direction ball will go when game starts
    private float currentBallSpeed;
    private float initialBallSpeed; // Store initial speed for reset
    
    // Boundaries
    private float halfWidth;
    private float halfHeight;
    private float paddleHalfHeight;
    private float ballRadius;
    
    // Game state
    private int player1Score = 0;
    private int player2Score = 0;
    private bool isWaitingForInput = true;  // Ball waits for paddle movement
    private bool isGameOver = false;
    private int winningPlayer = 0;
    
    // Flashing effect for win screen
    private float flashTimer = 0f;
    private bool flashState = true;
    
    // ===========================================
    // UNITY LIFECYCLE METHODS
    // ===========================================
    
    /// <summary>
    /// Start is called before the first frame update
    /// Initializes the game by creating all objects and setting up initial state
    /// </summary>
    void Start()
    {
        // Store initial ball speed for reset
        initialBallSpeed = ballSpeed;

        // Calculate half dimensions for easier boundary checks
        halfWidth = gameWidth / 2f;
        halfHeight = gameHeight / 2f;
        paddleHalfHeight = paddleHeight / 2f;
        ballRadius = ballSize / 2f;
        
        // Create all game objects
        CreatePaddles();
        CreateBall();
        CreateWalls();
        CreateScoreDisplays();
        CreateWinScreen();
        
        // Configure the camera to show the game area properly
        SetupCamera();
        
        // Set up initial round
        StartNewRound(0); // 0 = random direction
    }
    
    /// <summary>
    /// Update is called once per frame
    /// This is our main game loop - handles input, movement, and collisions
    /// </summary>
    void Update()
    {
        // If game is over (someone won), handle win screen
        if (isGameOver)
        {
            HandleWinScreenUpdate();
            return;
        }
        
        // If waiting for input, check if either player moved their paddle
        if (isWaitingForInput)
        {
            HandleWaitingForInput();
            return;
        }
        
        // Handle paddle movement based on player input
        HandlePaddleInput();
        
        // Move the ball and check for collisions
        MoveBall();
        
        // Check for scoring (ball passing paddles)
        CheckBallOutOfBounds();
    }
    
    // ===========================================
    // GAME STATE MANAGEMENT
    // ===========================================
    
    /// <summary>
    /// Handles waiting state - ball starts when a player moves
    /// </summary>
    void HandleWaitingForInput()
    {
        // Check if any player pressed their movement keys
        bool player1Moved = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
        bool player2Moved = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
        
        // Also allow paddle movement while waiting
        HandlePaddleInput();
        
        if (player1Moved || player2Moved)
        {
            // Start the ball moving
            isWaitingForInput = false;
            ballVelocity = pendingBallDirection * currentBallSpeed;
        }
    }
    
    /// <summary>
    /// Handles the win screen state - flashing text and restart input
    /// </summary>
    void HandleWinScreenUpdate()
    {
        // Animate flashing "PRESS SPACE" text
        flashTimer += Time.deltaTime;
        if (flashTimer >= 0.5f)
        {
            flashTimer = 0f;
            flashState = !flashState;
            
            // Find the "Press Space" text and toggle visibility
            Transform pressSpace = winScreen.transform.Find("PressSpace");
            if (pressSpace != null)
            {
                pressSpace.gameObject.SetActive(flashState);
            }
        }
        
        // Check for restart
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RestartGame();
        }
    }
    
    /// <summary>
    /// Called when a player scores
    /// </summary>
    /// <param name="scoringPlayer">1 for Player 1, 2 for Player 2</param>
    void OnPlayerScore(int scoringPlayer)
    {
        // Update score
        if (scoringPlayer == 1)
        {
            player1Score++;
        }
        else
        {
            player2Score++;
        }

        // Increase ball speed by 0.5 after each point
        ballSpeed += 0.5f;

        // Update score display
        UpdateScoreDisplays();
        
        // Check for win condition
        if (player1Score >= winningScore)
        {
            TriggerWin(1);
        }
        else if (player2Score >= winningScore)
        {
            TriggerWin(2);
        }
        else
        {
            // Start new round - ball goes toward the player who was scored on
            // If Player 1 scored, ball goes toward Player 2 (right), and vice versa
            int ballDirection = (scoringPlayer == 1) ? 1 : -1;
            StartNewRound(ballDirection);
        }
    }
    
    /// <summary>
    /// Starts a new round - resets ball and paddles to center
    /// </summary>
    /// <param name="ballDirection">-1 = left, 1 = right, 0 = random</param>
    void StartNewRound(int ballDirection)
    {
        // Reset paddles to center
        leftPaddle.transform.position = new Vector3(-halfWidth + paddleOffset, 0f, 0f);
        rightPaddle.transform.position = new Vector3(halfWidth - paddleOffset, 0f, 0f);
        
        // Reset ball to center
        ball.transform.position = Vector3.zero;
        ball.SetActive(true);
        
        // Reset ball speed
        currentBallSpeed = ballSpeed;
        
        // Determine ball direction
        float horizontalDir;
        if (ballDirection == 0)
        {
            horizontalDir = Random.value > 0.5f ? 1f : -1f;
        }
        else
        {
            horizontalDir = ballDirection;
        }
        
        // Calculate pending direction (ball will start moving when player moves)
        float verticalAngle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
        pendingBallDirection = new Vector2(
            horizontalDir * Mathf.Cos(verticalAngle),
            Mathf.Sin(verticalAngle)
        ).normalized;
        
        // Ball velocity is zero until player moves
        ballVelocity = Vector2.zero;
        isWaitingForInput = true;
    }
    
    /// <summary>
    /// Triggers win state for a player
    /// </summary>
    /// <param name="player">The winning player (1 or 2)</param>
    void TriggerWin(int player)
    {
        isGameOver = true;
        winningPlayer = player;
        
        // Hide the ball
        ball.SetActive(false);
        
        // Update win screen text
        UpdateWinScreenText(player);
        
        // Show win screen
        winScreen.SetActive(true);
    }
    
    /// <summary>
    /// Restarts the game to initial state
    /// </summary>
    void RestartGame()
    {
        isGameOver = false;
        winningPlayer = 0;
        player1Score = 0;
        player2Score = 0;

        // Reset ball speed to initial value
        ballSpeed = initialBallSpeed;

        // Hide win screen
        winScreen.SetActive(false);
        
        // Update score display
        UpdateScoreDisplays();
        
        // Start new round with random direction
        StartNewRound(0);
    }
    
    // ===========================================
    // UI CREATION
    // ===========================================
    
    /// <summary>
    /// Creates the score displays for both players
    /// </summary>
    void CreateScoreDisplays()
    {
        // Player 1 score (left side)
        player1ScoreDisplay = new GameObject("Player1Score");
        player1ScoreDisplay.transform.position = new Vector3(-halfWidth / 2f, halfHeight - 1.5f, 0f);
        
        // Player 2 score (right side)
        player2ScoreDisplay = new GameObject("Player2Score");
        player2ScoreDisplay.transform.position = new Vector3(halfWidth / 2f, halfHeight - 1.5f, 0f);
        
        // Initial score display
        UpdateScoreDisplays();
    }
    
    /// <summary>
    /// Updates the score display numbers
    /// </summary>
    void UpdateScoreDisplays()
    {
        // Clear old score displays
        foreach (Transform child in player1ScoreDisplay.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in player2ScoreDisplay.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Create new score numbers
        CreateScoreNumber(player1Score, player1ScoreDisplay.transform);
        CreateScoreNumber(player2Score, player2ScoreDisplay.transform);
    }
    
    /// <summary>
    /// Creates a large pixel-art number for score display
    /// </summary>
    void CreateScoreNumber(int number, Transform parent)
    {
        string numStr = number.ToString();
        float pixelSize = 0.25f;
        float digitSpacing = 6f * pixelSize;
        float totalWidth = numStr.Length * digitSpacing;
        float startX = -totalWidth / 2f + digitSpacing / 2f;
        
        for (int d = 0; d < numStr.Length; d++)
        {
            char digit = numStr[d];
            string pattern = GetPatternForChar(digit);
            
            if (pattern != null)
            {
                float digitX = startX + d * digitSpacing;
                
                for (int row = 0; row < 5; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        int index = row * 5 + col;
                        if (index < pattern.Length && pattern[index] == '1')
                        {
                            GameObject pixel = CreateRectangle("ScorePixel", pixelSize, pixelSize, Color.white);
                            pixel.transform.SetParent(parent);
                            pixel.transform.localPosition = new Vector3(
                                digitX + (col - 2) * pixelSize,
                                (2 - row) * pixelSize,
                                0f
                            );
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Creates the win screen
    /// </summary>
    void CreateWinScreen()
    {
        // Parent object for all win screen elements
        winScreen = new GameObject("WinScreen");
        winScreen.SetActive(false);
        
        // Semi-transparent dark overlay
        GameObject overlay = CreateRectangle("Overlay", gameWidth + 2f, gameHeight + 2f, new Color(0f, 0f, 0f, 0.85f));
        overlay.transform.SetParent(winScreen.transform);
        overlay.transform.localPosition = new Vector3(0f, 0f, -1f);
        overlay.GetComponent<SpriteRenderer>().sortingOrder = 100;
        
        // Decorative line above
        GameObject lineTop = CreateRectangle("LineTop", 12f, 0.15f, Color.yellow);
        lineTop.transform.SetParent(winScreen.transform);
        lineTop.transform.localPosition = new Vector3(0f, 4f, -1f);
        lineTop.GetComponent<SpriteRenderer>().sortingOrder = 101;
        
        // Decorative line below
        GameObject lineBottom = CreateRectangle("LineBottom", 12f, 0.15f, Color.yellow);
        lineBottom.transform.SetParent(winScreen.transform);
        lineBottom.transform.localPosition = new Vector3(0f, -1f, -1f);
        lineBottom.GetComponent<SpriteRenderer>().sortingOrder = 101;
        
        // "PRESS SPACE TO PLAY AGAIN" text
        CreatePixelText("PRESS SPACE", 0f, -3f, 0.25f, Color.white, winScreen.transform, 101, "PressSpace");
        
        // Trophy decorations
        CreatePixelText("X", -6f, 1.5f, 0.5f, Color.yellow, winScreen.transform, 101);
        CreatePixelText("X", 6f, 1.5f, 0.5f, Color.yellow, winScreen.transform, 101);
    }
    
    /// <summary>
    /// Updates the win screen to show the winning player
    /// </summary>
    void UpdateWinScreenText(int player)
    {
        // Remove old win text if it exists
        Transform oldText = winScreen.transform.Find("WinText");
        if (oldText != null)
        {
            Destroy(oldText.gameObject);
        }
        
        // Create "PLAYER X WINS" text
        string winText = "PLAYER " + player + " WINS";
        CreatePixelText(winText, 0f, 1.5f, 0.35f, Color.yellow, winScreen.transform, 101, "WinText");
    }
    
    /// <summary>
    /// Creates pixel-art style text using small rectangles
    /// Each letter is defined as a 5x5 grid pattern
    /// </summary>
    void CreatePixelText(string text, float x, float y, float scale, Color color, Transform parent, int sortingOrder, string objectName = null)
    {
        GameObject textObj = new GameObject(objectName ?? "PixelText_" + text);
        textObj.transform.SetParent(parent);
        textObj.transform.localPosition = new Vector3(x, y, -1f);
        
        float pixelSize = 0.15f * scale;
        float letterSpacing = 6f * pixelSize;
        float totalWidth = text.Length * letterSpacing;
        float startX = -totalWidth / 2f + letterSpacing / 2f;
        
        for (int c = 0; c < text.Length; c++)
        {
            char ch = text[c];
            string pattern = GetPatternForChar(ch);
            
            if (pattern != null)
            {
                float letterX = startX + c * letterSpacing;
                
                for (int row = 0; row < 5; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        int index = row * 5 + col;
                        if (index < pattern.Length && pattern[index] == '1')
                        {
                            GameObject pixel = CreateRectangle("Pixel", pixelSize, pixelSize, color);
                            pixel.transform.SetParent(textObj.transform);
                            pixel.transform.localPosition = new Vector3(
                                letterX + (col - 2) * pixelSize,
                                (2 - row) * pixelSize,
                                0f
                            );
                            pixel.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Returns the 5x5 pixel pattern for a character
    /// </summary>
    string GetPatternForChar(char c)
    {
        switch (char.ToUpper(c))
        {
            case 'A': return "01110100011111110001100011";
            case 'B': return "11110100011111010001111101";
            case 'C': return "01110100011000010001011101";
            case 'D': return "11110100011000110001111101";
            case 'E': return "11111100001111010000111111";
            case 'F': return "11111100001111010000100001";
            case 'G': return "01111100001001110001011111";
            case 'H': return "10001100011111110001100011";
            case 'I': return "11111001000010000100111111";
            case 'J': return "11111000100001010010011001";
            case 'K': return "10001100101110010010100011";
            case 'L': return "10000100001000010000111111";
            case 'M': return "10001110111010110001100011";
            case 'N': return "10001110011010110011100011";
            case 'O': return "01110100011000110001011101";
            case 'P': return "11110100011111010000100001";
            case 'Q': return "01110100011000101010011011";
            case 'R': return "11110100011111010010100011";
            case 'S': return "01111100000111000001111101";
            case 'T': return "11111001000010000100001001";
            case 'U': return "10001100011000110001011101";
            case 'V': return "10001100011000101010001001";
            case 'W': return "10001100011010111011100011";
            case 'X': return "10001010100010001010100011";
            case 'Y': return "10001010100010000100001001";
            case 'Z': return "11111000100010001000111111";
            case '0': return "01110100011001110001011101";
            case '1': return "00100011000010000100011111";
            case '2': return "01110100010001000100111111";
            case '3': return "11110000010011000001111101";
            case '4': return "10001100011111100001000011";
            case '5': return "11111100001111000001111101";
            case '6': return "01110100001111010001011101";
            case '7': return "11111000010001000100010001";
            case '8': return "01110100010111010001011101";
            case '9': return "01110100010111100001011101";
            case ' ': return "00000000000000000000000001";
            case '-': return "00000000000111000000000001";
            case '!': return "00100001000010000000001001";
            default: return null;
        }
    }
    
    // ===========================================
    // GAME OBJECT CREATION
    // ===========================================
    
    /// <summary>
    /// Creates both paddles as white rectangles
    /// </summary>
    void CreatePaddles()
    {
        // Create left paddle (Player 1 - controlled by W/S keys)
        leftPaddle = CreateRectangle("LeftPaddle", paddleWidth, paddleHeight, Color.white);
        leftPaddle.transform.position = new Vector3(-halfWidth + paddleOffset, 0f, 0f);
        
        // Create right paddle (Player 2 - controlled by Up/Down arrow keys)
        rightPaddle = CreateRectangle("RightPaddle", paddleWidth, paddleHeight, Color.white);
        rightPaddle.transform.position = new Vector3(halfWidth - paddleOffset, 0f, 0f);
    }
    
    /// <summary>
    /// Creates the ball as a white square
    /// </summary>
    void CreateBall()
    {
        ball = CreateRectangle("Ball", ballSize, ballSize, Color.white);
        ball.transform.position = Vector3.zero;
    }
    
    /// <summary>
    /// Creates walls at top and bottom for visual reference
    /// (Collision is handled in code, not by Unity physics)
    /// </summary>
    void CreateWalls()
    {
        // Top wall - thin line at the top
        topWall = CreateRectangle("TopWall", gameWidth, 0.1f, Color.gray);
        topWall.transform.position = new Vector3(0f, halfHeight, 0f);
        
        // Bottom wall - thin line at the bottom
        bottomWall = CreateRectangle("BottomWall", gameWidth, 0.1f, Color.gray);
        bottomWall.transform.position = new Vector3(0f, -halfHeight, 0f);
        
        // Create center line (dashed effect using multiple small rectangles)
        CreateCenterLine();
    }
    
    /// <summary>
    /// Creates a dashed center line for visual appeal
    /// </summary>
    void CreateCenterLine()
    {
        float dashHeight = 0.4f;
        float dashGap = 0.4f;
        float dashWidth = 0.1f;
        
        // Create dashes from bottom to top
        for (float y = -halfHeight + dashGap; y < halfHeight; y += dashHeight + dashGap)
        {
            GameObject dash = CreateRectangle("CenterDash", dashWidth, dashHeight, Color.gray);
            dash.transform.position = new Vector3(0f, y, 0f);
        }
    }
    
    /// <summary>
    /// Helper method to create a rectangle sprite
    /// </summary>
    GameObject CreateRectangle(string name, float width, float height, Color color)
    {
        // Create a new GameObject
        GameObject obj = new GameObject(name);
        
        // Add a SpriteRenderer component
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        
        // Create a simple white texture
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        // Create a sprite from the texture
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        renderer.sprite = sprite;
        renderer.color = color;
        
        // Scale to desired size
        obj.transform.localScale = new Vector3(width, height, 1f);
        
        return obj;
    }
    
    /// <summary>
    /// Sets up the camera to properly display the game area
    /// </summary>
    void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Set orthographic size to show the full game height
            mainCamera.orthographicSize = halfHeight;
            
            // Position camera at origin, looking at the game
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            
            // Set background to black for classic Pong look
            mainCamera.backgroundColor = Color.black;
        }
    }
    
    // ===========================================
    // INPUT HANDLING
    // ===========================================
    
    /// <summary>
    /// Handles player input for paddle movement
    /// Player 1 (Left paddle): W (up) / S (down)
    /// Player 2 (Right paddle): Up Arrow / Down Arrow
    /// </summary>
    void HandlePaddleInput()
    {
        // Calculate movement amount for this frame
        float moveAmount = paddleSpeed * Time.deltaTime;
        
        // --- PLAYER 1 - LEFT PADDLE (W/S keys) ---
        if (Input.GetKey(KeyCode.W))
        {
            MovePaddle(leftPaddle, moveAmount);
        }
        if (Input.GetKey(KeyCode.S))
        {
            MovePaddle(leftPaddle, -moveAmount);
        }
        
        // --- PLAYER 2 - RIGHT PADDLE (Arrow keys) ---
        if (Input.GetKey(KeyCode.UpArrow))
        {
            MovePaddle(rightPaddle, moveAmount);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            MovePaddle(rightPaddle, -moveAmount);
        }
    }
    
    /// <summary>
    /// Moves a paddle by the specified amount, clamping to stay within bounds
    /// </summary>
    void MovePaddle(GameObject paddle, float amount)
    {
        // Get current position
        Vector3 pos = paddle.transform.position;
        
        // Calculate new Y position
        float newY = pos.y + amount;
        
        // Clamp to keep paddle within game bounds
        float maxY = halfHeight - paddleHalfHeight;
        float minY = -halfHeight + paddleHalfHeight;
        newY = Mathf.Clamp(newY, minY, maxY);
        
        // Apply new position
        paddle.transform.position = new Vector3(pos.x, newY, pos.z);
    }
    
    // ===========================================
    // BALL MOVEMENT AND COLLISION
    // ===========================================
    
    /// <summary>
    /// Moves the ball based on its velocity and handles wall/paddle collisions
    /// </summary>
    void MoveBall()
    {
        // Get current ball position
        Vector3 pos = ball.transform.position;
        
        // Calculate new position based on velocity and time
        float newX = pos.x + ballVelocity.x * Time.deltaTime;
        float newY = pos.y + ballVelocity.y * Time.deltaTime;
        
        // --- CHECK TOP/BOTTOM WALL COLLISION ---
        if (newY + ballRadius > halfHeight)
        {
            newY = halfHeight - ballRadius;
            ballVelocity.y = -Mathf.Abs(ballVelocity.y);
        }
        else if (newY - ballRadius < -halfHeight)
        {
            newY = -halfHeight + ballRadius;
            ballVelocity.y = Mathf.Abs(ballVelocity.y);
        }
        
        // --- CHECK PADDLE COLLISION ---
        if (CheckPaddleCollision(leftPaddle, newX, newY, true))
        {
            newX = leftPaddle.transform.position.x + paddleWidth / 2f + ballRadius;
            BounceOffPaddle(leftPaddle, newY, true);
        }
        else if (CheckPaddleCollision(rightPaddle, newX, newY, false))
        {
            newX = rightPaddle.transform.position.x - paddleWidth / 2f - ballRadius;
            BounceOffPaddle(rightPaddle, newY, false);
        }
        
        // Apply new position
        ball.transform.position = new Vector3(newX, newY, pos.z);
    }
    
    /// <summary>
    /// Checks if the ball collides with a paddle
    /// </summary>
    bool CheckPaddleCollision(GameObject paddle, float ballX, float ballY, bool isLeftPaddle)
    {
        Vector3 paddlePos = paddle.transform.position;
        
        bool xCollision;
        if (isLeftPaddle)
        {
            xCollision = (ballX - ballRadius < paddlePos.x + paddleWidth / 2f) &&
                         (ballX + ballRadius > paddlePos.x - paddleWidth / 2f) &&
                         (ballVelocity.x < 0);
        }
        else
        {
            xCollision = (ballX + ballRadius > paddlePos.x - paddleWidth / 2f) &&
                         (ballX - ballRadius < paddlePos.x + paddleWidth / 2f) &&
                         (ballVelocity.x > 0);
        }
        
        bool yCollision = (ballY + ballRadius > paddlePos.y - paddleHalfHeight) &&
                          (ballY - ballRadius < paddlePos.y + paddleHalfHeight);
        
        return xCollision && yCollision;
    }
    
    /// <summary>
    /// Handles ball bouncing off a paddle
    /// </summary>
    void BounceOffPaddle(GameObject paddle, float ballY, bool isLeftPaddle)
    {
        float hitPosition = (ballY - paddle.transform.position.y) / paddleHalfHeight;
        hitPosition = Mathf.Clamp(hitPosition, -1f, 1f);
        
        float maxBounceAngle = 60f * Mathf.Deg2Rad;
        float bounceAngle = hitPosition * maxBounceAngle;
        
        currentBallSpeed = Mathf.Min(currentBallSpeed + ballSpeedIncrease, maxBallSpeed);
        
        float direction = isLeftPaddle ? 1f : -1f;
        ballVelocity.x = direction * Mathf.Cos(bounceAngle) * currentBallSpeed;
        ballVelocity.y = Mathf.Sin(bounceAngle) * currentBallSpeed;
    }
    
    /// <summary>
    /// Checks if ball has gone past a paddle (scoring condition)
    /// </summary>
    void CheckBallOutOfBounds()
    {
        float ballX = ball.transform.position.x;
        
        // Ball went past left paddle - Player 2 scores
        if (ballX < -halfWidth - ballRadius)
        {
            OnPlayerScore(2);
        }
        // Ball went past right paddle - Player 1 scores
        else if (ballX > halfWidth + ballRadius)
        {
            OnPlayerScore(1);
        }
    }
}
