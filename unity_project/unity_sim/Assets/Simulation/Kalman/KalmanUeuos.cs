using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;

public class KalmanUeuos : MonoBehaviour
{
    public const int EFFECT_BOOT = 0;
    public const int EFFECT_RAINBOW = 1;
    public const int STATE_OFF = 0;
    public const int STATE_AUTONOMY = 1;
    public const int STATE_TELEOP = 2;
    public const int STATE_FINISHED = 3;

    private static Func<float, Color> bootColorFunction = time => {
        const int numCycles = 10;
        const float cycleSpeed = 20;
        const float cycleSteepness = 3;
        float scaledX = time * cycleSpeed;
        if (scaledX < 2*Mathf.PI * numCycles)
        {
            // Only light up for the first 5 cycles.
            float sine = Mathf.Sin(scaledX - Mathf.PI/2);
            float sCurve = 1 / (1 + Mathf.Exp(-sine * cycleSteepness));
            return Color.red * sCurve;
        } else {
            return Color.black;
        }
    };

    [SerializeField]
    private string autonomyMaterialNameRegex = "autonomy";
    [SerializeField]
    private float maxIntensity = 10.0F;
    [SerializeField]
    private Material autonomyMaterial;
    private Func<float, Color> colorFunction;
    private float colorFunctionTimer;

    private async void Start()
    {
        // Get the autonomy material and clone it.
        var renderer = GetComponent<Renderer>();
        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (Regex.IsMatch(materials[i].name, autonomyMaterialNameRegex))
            {
                autonomyMaterial = materials[i];
                break;
            }
        }

        // Set default color.
        // colorFunction = bootColorFunction;
    }

    public void SetColor(Color color)
    {
        colorFunction = _ => color;
    }

    public void SetState(int state)
    {
        switch (state) {
            case STATE_OFF:
                colorFunction = _ => Color.black;
                break;
            case STATE_AUTONOMY:
                colorFunction = _ => Color.red;
                break;
            case STATE_TELEOP:
                colorFunction = _ => Color.blue;
                break;
            case STATE_FINISHED:
                colorFunction = time => {
                    float strength = Mathf.Sin(time * 2) * 0.5F + 0.5F;
                    return Color.green * strength;
                };
                colorFunctionTimer = 0;
                break;
            default:
                break;
        }
    }

    public void SetEffect(int effect)
    {
        switch (effect) {
            case EFFECT_BOOT:
                colorFunction = bootColorFunction;
                colorFunctionTimer = 0;
                break;
            case EFFECT_RAINBOW:
                colorFunction = time => {
                    float hue = (0.2F * time) % 1;
                    return Color.HSVToRGB(hue, 1, 1);
                };
                colorFunctionTimer = 0;
                break;
            default:
                break;
        }
    }

    private void Update()
    {
        // Add to the timer.
        colorFunctionTimer += Time.deltaTime;

        // Update the color.
        if (colorFunction != null)
        {
            // Shader is URP/Lit.
            autonomyMaterial.SetColor("_EmissionColor", colorFunction(colorFunctionTimer) * maxIntensity);
        }
    }
}
