﻿using AuroraEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

[ExecuteInEditMode]
public class AreaManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Transform parent in transform)
        {
            // Ignore the models parent
            if (parent.name == "Models")
            {
                continue;
            }

            foreach (Transform instance in parent)
            {
                AuroraInstance aInstance = instance.GetComponent<AuroraInstance>();
                if (aInstance == null)
                {
                    aInstance = instance.gameObject.AddComponent<AuroraInstance>();
                    aInstance.Initialize(instance.GetComponent<AuroraObject>().gitData);
                }
            }
        }
    }

    public AuroraGIT CreateGIT()
    {
        AuroraGIT git = new AuroraGIT();

        foreach (Transform parent in transform)
        {
            // Ignore the models parent
            if (parent.name == "Models")
            {
                continue;
            }

            foreach (Transform instance in parent)
            {
                AuroraInstance aInstance = instance.GetComponent<AuroraInstance>();

                aInstance.BuildGIT(git);
            }
        }

        return git;
    }
}
