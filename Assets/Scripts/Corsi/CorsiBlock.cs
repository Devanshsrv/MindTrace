using UnityEngine;                  // importing Unity engine functions
using UnityEngine.UI;               // importing UI system for Image component
using System.Collections;           // needed for IEnumerator coroutines

public class CorsiBlock : MonoBehaviour
{
    public int blockID;             // unique ID of this block (1–9)

    private Image image;            // reference to the block's Image component
    private Color defaultColor;     // stores the default color of the block

    public Color highlightColor = Color.cyan;  // color used when block lights up

    private CorsiGameManager gameManager;      // reference to the GameManager

    void Start()
    {
        image = GetComponent<Image>();         // get the Image component attached to this block
        defaultColor = image.color;            // save the starting color of the block

        gameManager = FindObjectOfType<CorsiGameManager>(); // find the GameManager in the scene
    }

    public void Highlight()
    {
        image.color = highlightColor;          // change block color to cyan when highlighted
    }

    public void ResetColor()
    {
        image.color = defaultColor;            // change block color back to original
    }

    public void OnBlockClicked()
{
    if (gameManager.IsPlayerTurn())
    {
        gameManager.PlayerClicked(blockID, this);
    }
}
   public IEnumerator ClickFlash(Color flashColor)
{
    image.color = flashColor;
    yield return new WaitForSeconds(0.25f);
    image.color = defaultColor;
}
}