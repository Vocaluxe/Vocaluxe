using System;
using System.Collections.Generic;
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
        return CreateElementBase(screenInfo, screenName);
    }

    private GameObject CreateElement(CUiElementButton buttonInfo)
    {
        var button = CreateElementBase(buttonInfo);
        button.AddComponent<Button>();
        Button btnComponent = button.GetComponent<Button>();
        btnComponent.onClick.AddListener(buttonInfo.Controller.RaiseClicked);
        return button;
    }

    private GameObject CreateElement(CUiElementText textInfo)
    {
        return CreateElementBase(textInfo);
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

    private GameObject CreateElementBase(CUiElement element, string elementName = "")
    {
        GameObject newGameObject = new GameObject(name: elementName);

        newGameObject.AddComponent<RectTransform>();
        RectTransform rectTransform = newGameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition.Set(0f,0f);
        rectTransform.rect.position.Set(element.LayoutX, element.LayoutY);
        rectTransform.rect.size.Set(element.LayoutWidth, element.LayoutHeight);

        foreach (YogaNode child in element)
        {
            var newObj = CreateElement(child);
            newObj.GetComponent<RectTransform>().SetParent(newGameObject.transform);
        }

        return newGameObject;
    }
}
