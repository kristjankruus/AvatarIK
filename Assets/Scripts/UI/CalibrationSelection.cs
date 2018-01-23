using System;
using UnityEngine;

[Serializable]
public class CalibrationSelection : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private CalibrationSelectionObject _selectionObject;
    private InitialCalibration _initialCalibration;

    private void Start()
    {
        _initialCalibration = new InitialCalibration();
        PrefillCalibration();
    }

    private void PrefillCalibration()
    {
        //foreach (var calibration in _initialCalibration.GetCalibrations())
        //{
        //    var calibrationSelection = Instantiate(_selectionObject);
        //    calibrationSelection.transform.SetParent(_panel.transform, false);
        //    calibrationSelection.transform.localScale = new Vector3(1, 1, 1);
        //    calibrationSelection.Text.text = calibration.Description;

        //    if (!string.IsNullOrEmpty(calibration.Image))
        //    {
        //        calibrationSelection.Image.sprite = Resources.Load<Sprite>(calibration.Image);
        //    }

        //    calibrationSelection.GetComponent<Button>().onClick.AddListener(() => ChooseCalibration(calibration));
        //}
    }

    //private void ChooseCalibration(Calibration calibration)
    //{
    //    _character.SetPosition();
    //}
}
