using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StartScreenManager : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TMP_InputField ageInput;
    public static int playerAge;
    public Button startButton;
    public GameObject startPanel;
    public CorsiGameManager gameManager;

    public static string playerName;

    void Start()
    {
        startButton.interactable = false;
    }

    void Update()
    {
        // Get current input text safely
        string currentText = nameInput.text;

        if (nameInput.isFocused && nameInput.textComponent != null)
        {
            currentText = nameInput.textComponent.text;
        }

        // Enable button only if valid input
        string nameText = nameInput.text;
        string ageText = ageInput.text;

        bool valid = !string.IsNullOrWhiteSpace(nameText) &&
                     int.TryParse(ageText, out _);

        startButton.interactable = valid;

        // Mobile fix: ensure input field gets focus on tap
        if (Input.GetMouseButtonDown(0))
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                nameInput.GetComponent<RectTransform>(),
                Input.mousePosition,
                null))
            {
                nameInput.ActivateInputField();
            }
        }
    }

    public void StartGame()
    {
        string inputName = nameInput.text;

        if (string.IsNullOrWhiteSpace(inputName))
        {
            Debug.Log("Please enter a name");
            return;
        }

        // Normalize name
        inputName = inputName.ToLower().Trim();
        inputName = char.ToUpper(inputName[0]) + inputName.Substring(1);

        playerName = inputName;
        playerAge = int.Parse(ageInput.text);

        // Close start screen and begin game
        startPanel.SetActive(false);
        gameManager.BeginGame();
    }
}