using UnityEngine;
using UnityEngine.UI;
using LeiaUnity;
using System.Collections;
public class DisplayCameraCentricSwitcher : MonoBehaviour
{
    #region Properties
    [Header("Camera Settings")]
    public GameObject[] cameras;
    public LeiaDisplay displayCentric;
    public LeiaDisplay cameraCentric;

    [Header("UI Settings")]
    public LeiaUnity.Examples.LeiaDisplaySettingsCanvas settingCanvasDisplayCentric;
    public LeiaUnity.Examples.LeiaDisplaySettingsCanvas settingCanvasCameraCentric;
    public Slider[] DisplayCentricOnlySliders;
    public Slider[] CameraCentricOnlySliders;
    public Button DisplayCentricPanelButton;
    public Button CameraCentricPanelButton;
    public Color interactableSliderColor = Color.white;
    public Color nonInteractableSliderColor = Color.black;

    [Header("Label Settings")]
    public Text DisplayCentricSettingLabel;
    public Text CameraCentricSettingLabel;

    [Header("Slider Settings")]
    public Slider FocalDistanceSlider;
    public Slider FoVSlider;
    public Slider VirtualHeightSlider;
    public Slider FOVFactorSlider;
    #endregion

    private void Start()
    {
        InitializeCameras();
        ConfigureUI();
    }

    #region UI Configuration
    private void ConfigureUI()
    {
        SetSliderInteractivity(true);
        UpdateSliderColors();
        ConfigureButtonListeners();
        SetInitialLabelColors();
    }

    private void ConfigureButtonListeners()
    {
        DisplayCentricPanelButton.onClick.AddListener(SwitchToDisplayCentric);
        CameraCentricPanelButton.onClick.AddListener(SwitchToCameraCentric);
    }

    private void SetInitialLabelColors()
    {
        DisplayCentricSettingLabel.color = Color.white;
        CameraCentricSettingLabel.color = Color.gray;
    }
    #endregion

    #region Camera Methods
    private void InitializeCameras()
    {
        // Initialize cameras based on array length
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].SetActive(i == 0);
        }
    }

    private void SetActiveCamera(int index)
    {
        // Activate the selected camera and deactivate others
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].SetActive(i == index);
        }
    }
    #endregion

    #region Slider Methods
    private void SetSliderInteractivity(bool isInteractable)
    {
        UpdateSliders(DisplayCentricOnlySliders, isInteractable);
        UpdateSliders(CameraCentricOnlySliders, !isInteractable);
    }

    private void UpdateSliders(Slider[] sliders, bool isInteractable)
    {
        foreach (var slider in sliders)
        {
            slider.interactable = isInteractable;
            UpdateSliderColor(slider, isInteractable ? interactableSliderColor : nonInteractableSliderColor);
        }
    }

    private void UpdateSliderColor(Slider slider, Color color)
    {
        Image[] sliderComponents = slider.GetComponentsInChildren<Image>();
        foreach (var component in sliderComponents)
        {
            component.color = color;
        }
    }

    private void UpdateSliderColors()
    {
        // Update colors for all sliders
        UpdateSliders(DisplayCentricOnlySliders, DisplayCentricOnlySliders[0].interactable);
        UpdateSliders(CameraCentricOnlySliders, CameraCentricOnlySliders[0].interactable);
    }
    #endregion

    #region Switch Panel Methods
    public void SwitchToDisplayCentric()
    {
        SetActiveCamera(0);
        SetSliderInteractivity(true);

        // Handle display centric operations
        StartCoroutine(HandleDisplayCentric());
        StartCoroutine(UpdateUIAndLabels());
    }

    public void SwitchToCameraCentric()
    {
        SetActiveCamera(1);
        SetSliderInteractivity(false);

        // Handle camera centric operations
        StartCoroutine(HandleCameraCentric());
        StartCoroutine(UpdateUIAndLabels());
    }

    IEnumerator UpdateUIAndLabels()
    {
        yield return new WaitForSeconds(0.1f);

        settingCanvasDisplayCentric.UpdateUI();
        settingCanvasCameraCentric.UpdateUI();
        UpdateSliderColors();
        UpdateLabelColors();
    }

    private void UpdateLabelColors()
    {
        DisplayCentricSettingLabel.color = DisplayCentricOnlySliders[0].interactable ? Color.white : Color.gray;
        CameraCentricSettingLabel.color = CameraCentricOnlySliders[0].interactable ? Color.white : Color.gray;
    }
    #endregion

    #region Camera Adjustment Handlers
    private IEnumerator HandleDisplayCentric()
    {
        yield return new WaitForSeconds(0.1f);

        HandleVirtualHeight();
        HandleZPositionChange();
        HandleFoVFactor();
    }

    private IEnumerator HandleCameraCentric()
    {
        yield return new WaitForSeconds(0.1f);

        HandleFocalDistance();
        HandleFoV();
    }

    public void HandleZPositionChange()
    {
        cameraCentric.DriverCamera.transform.position = displayCentric.transform.position - new Vector3(0, 0, (displayCentric.ViewingDistanceMM * displayCentric.MMToVirtual) / displayCentric.FOVFactor);
    }

    public void HandleVirtualHeight()
    {
        Vector3 displayPosition = displayCentric.gameObject.transform.position;
        Vector3 cameraPosition = displayCentric.transform.position - new Vector3(0, 0, (displayCentric.ViewingDistanceMM * displayCentric.MMToVirtual) / displayCentric.FOVFactor);
        cameraCentric.FocalDistance = Vector3.Distance(displayPosition, cameraPosition);
        FocalDistanceSlider.value = Vector3.Distance(displayPosition, cameraPosition);
    }

    public void HandleFocalDistance()
    {
        displayCentric.gameObject.transform.position = new Vector3(0, 0, cameraCentric.FocalDistance) + cameraCentric.DriverCamera.transform.position;
        displayCentric.VirtualHeight = cameraCentric.VirtualHeight;
        VirtualHeightSlider.value = cameraCentric.VirtualHeight;
    }

    public void HandleFoVFactor()
    {
        float fov = Mathf.Atan2(displayCentric.FOVFactor * (displayCentric.HeightMM / 2f), displayCentric.ViewingDistanceMM) * Mathf.Rad2Deg * 2f;
        cameraCentric.DriverCamera.fieldOfView = fov;
        FoVSlider.value = fov;
    }

    public void HandleFoV()
    {
        float fovFactor = (cameraCentric.ViewingDistanceMM * cameraCentric.VirtualHeight) / (cameraCentric.HeightMM * cameraCentric.transform.localPosition.z);
        displayCentric.FOVFactor = fovFactor;
        FOVFactorSlider.value = fovFactor;
    }
    #endregion
}