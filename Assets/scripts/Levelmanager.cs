using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Saare Levels, Sequence Mein (Level 1, Level 2, ...)")]
    public LevelConfig[] allLevels;

    [Header("References")]
    public PieceGridRenderer gridRenderer;
    public ShapeInfoDisplay shapeInfoDisplay;

    [Header("Level Number Display")]
    public TextMeshProUGUI levelNumberText;      // "Level X" (Text)
    public GameObject levelNumberPanel;          // Poora panel jisme text + background image hai
    public GameObject levelCrushedImage;         // "Level Crushed" (Image)

    [Header("Next Button")]
    public Button nextLevelButton;
    public TMP_Text nextLevelButtonLabel;

    private const string SavedLevelIndexKey = "LastPlayedLevelIndex";

    private int currentLevelIndex = 0;

    public int CurrentLevelIndex => currentLevelIndex;

  

    private void Start()
    {
        int savedIndex = PlayerPrefs.GetInt(SavedLevelIndexKey, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, allLevels.Length - 1);
        LoadLevel(savedIndex);
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= allLevels.Length)
        {
            Debug.LogWarning("LevelManager: is index ka level maujood nahi.");
            return;
        }

        currentLevelIndex = index;
        LevelConfig config = allLevels[index];

        gridRenderer.boardData = config.board;
        gridRenderer.availablePieces = config.pieces;
        gridRenderer.RebuildLevel();

        if (shapeInfoDisplay != null)
            shapeInfoDisplay.BuildShapeIcons();

        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = false;
        }

        // Naya level shuru - Level Number dikhao, Crushed Image chhupao
        ShowLevelNumber(config);

        UpdateNextButtonLabel();
        SaveProgress();

        gridRenderer.onBoardComplete.RemoveListener(OnLevelSolved);
        gridRenderer.onBoardComplete.AddListener(OnLevelSolved);
    }

    // Level number dikhane ke liye
    private void ShowLevelNumber(LevelConfig config)
    {
        if (levelNumberPanel != null)
        {
            levelNumberPanel.SetActive(true);
        }

        if (levelNumberText != null)
        {
            levelNumberText.text = "Level " + config.levelNumber;
        }

        if (levelCrushedImage != null)
        {
            levelCrushedImage.SetActive(false);
        }
    }
    // Level Crushed Image dikhane ke liye
    private void ShowLevelCrushed()
    {
        if (levelNumberPanel != null)
        {
            levelNumberPanel.SetActive(false);
        }

        if (levelCrushedImage != null)
        {
            levelCrushedImage.SetActive(true);
        }
    }

    private void UpdateNextButtonLabel()
    {
        if (nextLevelButtonLabel == null) return;

        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < allLevels.Length)
        {
            int nextLevelNumber = allLevels[nextIndex].levelNumber;
            nextLevelButtonLabel.text = "Next | Lvl " + (nextLevelNumber + 1);
        }
        else
        {
            nextLevelButtonLabel.text = "All Levels Complete!";
        }
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(SavedLevelIndexKey, currentLevelIndex);
        PlayerPrefs.Save();
    }

    private void OnLevelSolved()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = true;
        }

        // Level Crushed Image dikhao
        ShowLevelCrushed();
    }

    public void LoadNextLevel()
    {
        int next = currentLevelIndex + 1;
        if (next >= allLevels.Length)
        {
            Debug.Log("Saare levels complete ho chuke!");
            return;
        }
        LoadLevel(next);
    }

    public void ReloadCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }
}