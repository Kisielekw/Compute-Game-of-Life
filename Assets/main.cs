using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class main : MonoBehaviour
{
    public ComputeShader compute;

    public int StepsPerFixedFrame;
    public int Scale;
    
    private ComputeBuffer cellBuffer;
    private ComputeBuffer newCellBuffer;
    private RenderTexture output;

    private int[] cellArray;
    private int2 size;
    private bool play;

    private void Start()
    {
        play = false;
        size = new int2(16, 9) * Scale;
        Init();
    }

    private void Update()
    {
        compute.SetBool("play", play);
        newCellBuffer.GetData(cellArray);

        if (Input.GetMouseButtonDown((int)MouseButton.Left))
        {
            Vector2 mousePos = Input.mousePosition;
            mousePos /= new Vector2(Screen.width, Screen.height);
            mousePos *= new Vector2(size.x, size.y);
            int index = (int)mousePos.y * size.x + (int)mousePos.x;
            if (cellArray[index] == 0) cellArray[index] = 1;
            else cellArray[index] = 0;
        }

        cellBuffer.SetData(cellArray);
        if (!play) compute.Dispatch(0, Mathf.CeilToInt((float)size.x / 8), Mathf.CeilToInt((float)size.y / 8), 1);
    }

    private void FixedUpdate()
    {
        if (play)
        {
            for (int i = 0; i < StepsPerFixedFrame; i++)
            {
                compute.Dispatch(0, Mathf.CeilToInt((float)size.x / 8), Mathf.CeilToInt((float)size.y / 8), 1);
            }
            newCellBuffer.GetData(cellArray);
            cellBuffer.SetData(cellArray);
        }
    }

    private void OnGUI()
    {
        if(GUI.Button(new Rect(20, 40, 160, 50), "Pause/Play"))
        {
            play = !play;
        }
    }

    private void Init()
    {
        InitTexture();
        InitArray();
        SetComputeData();
    }

    private void InitTexture()
    {
        if(output == null || output.width != size.x || output.height != size.y)
        {
            if(output != null) output.Release();

            output = new RenderTexture(size.x, size.y, 0);
            output.enableRandomWrite = true;
            output.filterMode = FilterMode.Point;
            output.Create();

            GameObject quad = GameObject.Find("Quad");
            quad.GetComponent<MeshRenderer>().material.mainTexture = output;
        }
    }

    private void InitArray()
    {
        cellArray = new int[size.x * size.y];
    }

    private void SetComputeData()
    {
        cellBuffer = new ComputeBuffer(size.x * size.y, sizeof(int));
        cellBuffer.SetData(cellArray);
        newCellBuffer = new ComputeBuffer(size.x * size.y, sizeof(int));

        compute.SetBuffer(0, "cellBuffer", cellBuffer);
        compute.SetBuffer(0, "newCellBuffer", newCellBuffer);
        compute.SetTexture(0, "Result", output);
        compute.SetInt("width", size.x);
        compute.SetInt("height", size.y);
    }
}
