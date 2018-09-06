using System;
using System.Collections.Generic;
using System.Linq;
using Facebook.Yoga;
using UnityEngine;
using UnityEngine.UI;
using Vocaluxe.UI;
using Vocaluxe.UI.AbstractElements;
using Vocaluxe.UI.BasicElements.Button;
using Vocaluxe.UI.BasicElements.Text;
using Vocaluxe.UI.Screens;

public class ScreenHandler : MonoBehaviour
{
    private CUiConnector _connector;
    private Dictionary<EUiScreenType, GameObject> _screens = new Dictionary<EUiScreenType, GameObject>();
    private EUiScreenType _currentScreen = EUiScreenType.None;

    public GameObject ScreenPrefab;
    public GameObject ButtonPrefab;
    public GameObject TextPrefab;
    

    // Use this for initialization
    void Start ()
    {
        useGUILayout = false;
        RectTransform rt = (RectTransform)transform;
        _connector = new CUiConnector(Convert.ToInt32(rt.rect.width), Convert.ToInt32(rt.rect.height));
        foreach (var screenInfo in _connector.GetAllScreens())
        {
            _CreateScreen(screenInfo);
        }
    }

	// Update is called once per frame
	void Update ()
	{
	    if (_currentScreen == DummyClass.CurrentScreen)
	        return;

	    ChangeScreen(_currentScreen, DummyClass.CurrentScreen);
	}

    private void ChangeScreen(EUiScreenType currentScreen, EUiScreenType targetScreen)
    {
        if (currentScreen == targetScreen)
            return;
        DeactivateScreen(currentScreen);
        ActivateScreen(targetScreen);
    }

    private void ActivateScreen(EUiScreenType screen)
    {
        if(screen == EUiScreenType.None)
            return;
        _screens[screen].SetActive(true);
        _currentScreen = screen;
    }

    private void DeactivateScreen(EUiScreenType screen)
    {
        if (screen == EUiScreenType.None)
            return;
        _screens[screen].SetActive(false);
        _currentScreen = DummyClass.CurrentScreen;
    }

    private void _CreateScreen(KeyValuePair<EUiScreenType, CUiScreen> screenNameAndInfo)
    {
        GameObject newScreen = CreateElement(screenNameAndInfo.Value, screenNameAndInfo.Key.ToString());
        _screens.Add(screenNameAndInfo.Key, newScreen);
        newScreen.GetComponent<RectTransform>().SetParent(this.transform);
        newScreen.SetActive(false);
    }

    private GameObject CreateElement(CUiScreen screenInfo, string screenName = "")
    {
        GameObject screen = Instantiate(ScreenPrefab);
        screen.name = screenName;
        SetProperties(screenInfo, screen);
        return screen;
    }

    private void SetProperties(CUiScreen screenInfo, GameObject screen)
    {
        PositionElement(screenInfo, screen);
        AddChildren(screenInfo, screen);
    }

    private GameObject CreateElement(CUiElementButton buttonInfo)
    {
        var button = Instantiate(ButtonPrefab);
        SetProperties(buttonInfo, button);
        return button;
    }

    private void SetProperties(CUiElementButton buttonInfo, GameObject button)
    {
        Button btnComponent = button.GetComponent<Button>();
        btnComponent.onClick.AddListener(buttonInfo.Controller.RaiseClicked);
        PositionElement(buttonInfo, button);

        // TODO: Remove: making test buttons green
        button.GetComponent<Image>().color = Color.green;

        var buttonText = button.GetComponentInChildren<Text>().gameObject;
        SetProperties((CUiElementText)buttonInfo.AsQueryable().FirstOrDefault(x => x is CUiElementText), buttonText);
    }

    private GameObject CreateElement(CUiElementText textInfo)
    {
        var text = Instantiate(TextPrefab);
        SetProperties(textInfo, text);
        return text;
    }

    private void SetProperties(CUiElementText textInfo, GameObject text)
    {
        PositionElement(textInfo, text);
    }

    private GameObject CreateElement(YogaNode node)
    {
        var screen = node as CUiScreen;
        if (screen != null)
        {
            return CreateElement(screen);
        }

        var button = node as CUiElementButton;
        if (button != null)
        {
            return CreateElement(button);
        }

        var text = node as CUiElementText;
        if (text != null)
        {
            return CreateElement(text);
        }

        throw new NotSupportedException();
    }

    private void AddChildren(CUiElement uiElement, GameObject gameObj)
    {
        foreach (YogaNode child in uiElement)
        {
            var newObj = CreateElement(child);
            newObj.GetComponent<RectTransform>().SetParent(gameObj.transform);
        }
    }

    private static void PositionElement(CUiElement element, GameObject newGameObject)
    {
        RectTransform rectTransform = newGameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition.Set(0f, 0f);
        rectTransform.rect.position.Set(element.LayoutX, element.LayoutY);
        rectTransform.rect.size.Set(element.LayoutWidth, element.LayoutHeight);
    }
}
