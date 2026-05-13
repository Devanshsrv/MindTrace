// MenuManager.cs
// Attach this to a GameObject called "MenuManager" in the MainMenu scene.
//
// WHAT THIS DOES:
//   Shows a form where the player enters Name, Age, Education years.
//   When they press Start, it saves to GameData and loads Task 1.
//
// SCENE HIERARCHY NEEDED:
//   Canvas
//     Panel_Form
//       InputField_Name     <-- TMP InputField
//       InputField_Age      <-- TMP InputField (set ContentType = IntegerNumber)
//       InputField_Edu      <-- TMP InputField (set ContentType = IntegerNumber)
//       Text_Error          <-- TMP Text (red, hidden by default)
//     Button_Start          <-- Button
//     Text_Title            <-- TMP Text showing "MindTrace"

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Input Fields - drag from Hierarchy")]
    public TMP_InputField inputName;
    public TMP_InputField inputAge;
    public TMP_InputField inputEdu;

    [Header("Other UI")]
    public TMP_Text textError;   // drag the error text here

    // Called by the Start button's OnClick in the Inspector
    public void OnStartButtonClicked()
    {
        // Hide previous errors
        if (textError != null) textError.gameObject.SetActive(false);

        // Read name (optional - use "Player" if empty)
        string name = inputName != null ? inputName.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) name = "Player";

        // Validate age
        if (!float.TryParse(inputAge != null ? inputAge.text : "", out float age)
            || age < 10f || age > 100f)
        {
            ShowError("Please enter a valid age (10 to 100).");
            return;
        }

        // Validate education
        if (!float.TryParse(inputEdu != null ? inputEdu.text : "", out float edu)
            || edu < 0f || edu > 30f)
        {
            ShowError("Please enter valid education years (0 to 30).");
            return;
        }

        // Save to GameData
        GameData.Reset();
        GameData.PlayerName     = name;
        GameData.PlayerAge      = age;
        GameData.PlayerEduYears = edu;

        // Go to Task 1 (Word task)
        SceneManager.LoadScene("Stroop_Word");
    }

    void ShowError(string msg)
    {
        if (textError == null) return;
        textError.text = msg;
        textError.gameObject.SetActive(true);
    }
}
