using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private Character character;
    
    private void Awake()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<BattleUIManager>();
            
        }
        
        if (character == null)
        {
            character = GetComponent<Character>();
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager != null && character != null)
        {
            uiManager.OnCharacterHover(character);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.OnCharacterExit();
        }
    }
}