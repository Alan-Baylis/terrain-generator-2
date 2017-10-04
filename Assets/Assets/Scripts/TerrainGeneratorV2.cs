﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneratorV2 : MonoBehaviour {

	/*
	[Header("Game Objects")]
	public Terrain defaultLand;
	public GameObject water;
	*/

	[Header("General Settings")]
	public int size = 1024;
	public int height = 100;
	public float seedLand = 5000f;
	public bool useRandomSeed = true;
	public bool resetTerrainParams = false;

	[Header("Terrain Settings")]
	public bool createLargeMountains = false;
	public bool createLargeOceans = false;
	[Range(0.00f, 1.00f)]
	public float mountainSize = .0f;
	[Range(0.0000f, 0.0100f)]
	public float landExaggeration = .0009765625f;
	[Range(0.00000f, 0.00500f)]
	public float oceanSize = .0009765625f;
	[Range(0.0000f, 0.0500f)]
	public float frequency = .0009765625f;
	[Range(0.00f, 1.00f)]
	public float o1 = .5f;
	[Range(0.00f, 1.00f)]
	public float o2 = .5f;
	[Range(0.00f, 1.00f)]
	public float o3 = .5f;
	[Range(0.00f, 1.00f)]
	public float o4 = .5f;
	[Range(0.00f, 1.00f)]
	public float o5 = .5f;
	[Range(0.00f, 1.00f)]
	public float o6 = .5f;
	[Range(0.00f, 10.00f)]
	public float power = 1f;


	[Header("Texture Settings")]
	public bool useDefaultTextureHeights = true;
	public int seaLevel = 10;
	public int grassLevel = 20;
	public int rockLevel = 50;
	public int snowLevel = 90;

	[Header("Prototype Texture Indicies")]
	public int nullIndex = 5;
	public int waterIndex = 6;
	public int sandIndex = 0;
	public int grassIndex = 1;
	public int rockIndex = 2;
	public int snowIndex = 3;

	// Use this for initialization
	public void Start()
	{
		//if the user wants a random seed every time
		if (useRandomSeed) seedLand = (int)(Random.value*100000f);
		if (useDefaultTextureHeights)
		{
			seaLevel = Mathf.RoundToInt(height/5.3684f);
			grassLevel = Mathf.RoundToInt(height/5.1000f);
			rockLevel = Mathf.RoundToInt(height/3.900f);
			snowLevel = Mathf.RoundToInt(height/2.5000f);
		};
		Terrain thisTerrain = Terrain.activeTerrain;
		//Terrain t = (Terrain) Instantiate(defaultLand,Vector3.zero,Quaternion.identity);
		if (resetTerrainParams)
		{
			thisTerrain.terrainData.alphamapResolution = size;
			thisTerrain.terrainData.heightmapResolution = size;
			thisTerrain.terrainData.SetDetailResolution(size,16);
			thisTerrain.terrainData.size = new Vector3(size,height,size);
			float[,,] reset = new float[size,size,thisTerrain.terrainData.alphamapLayers];
			for (int z=0; z<size;z++)
			{
				for (int x=0; x<size; x++)
				{
					for (int s=0; s<thisTerrain.terrainData.alphamapLayers; s++)
					{
						reset[x,z,s] = 0;
					}
					//reset[x,z,0] = 1;
				}
			}
			thisTerrain.terrainData.SetAlphamaps(0,0,reset);
		}
		thisTerrain.terrainData.SetHeights(0,0,generateHeights(thisTerrain));
		float[,,] splatmap = generateSplatmap(thisTerrain);
		thisTerrain.terrainData.SetAlphamaps(0,0,splatmap);
		thisTerrain.GetComponent<TerrainCollider>().terrainData = thisTerrain.terrainData;
		thisTerrain.Flush();

	}
		
	float[,] generateHeights(Terrain t)
	{
		float[,] heights = new float[size,size];
		for (int z=0; z<size; z++)
		{
			for (int x=0; x<size; x++)
			{
				float xCoord = (float)(seedLand+x);
				float zCoord = (float)(seedLand+z);

				//put detail into the heightmap
				float e = (
					(o1 * Mathf.PerlinNoise(1*xCoord*frequency,1*zCoord*frequency)) + 
					(o2 * Mathf.PerlinNoise(2*xCoord*frequency,2*zCoord*frequency)) + 
					(o3 * Mathf.PerlinNoise(4*xCoord*frequency,4*zCoord*frequency)) + 
					(o4 * Mathf.PerlinNoise(8*xCoord*frequency,8*zCoord*frequency)) + 
					(o5 * Mathf.PerlinNoise(16*xCoord*frequency,16*zCoord*frequency)) + 
					(o6 * Mathf.PerlinNoise(32*xCoord*frequency,32*zCoord*frequency)));

				e /= (o1+o2+o3+o4+o5+o6);
				float mountainMultiplier = 1f;
				if (createLargeMountains)
				{
					mountainMultiplier = Mathf.PerlinNoise(xCoord*landExaggeration,zCoord*landExaggeration) + mountainSize;
				}
				e = Mathf.Pow(e,power*mountainMultiplier);

				//add height so we can add seas later
				//Elevation above sea-level
				float easl = e;
				e+=((float)grassLevel/(float)height);

				//generate sea
				//generate rivers

				heights[z,x] = e;
			}
		}
		return heights;
	}

	float[,,] generateSplatmap(Terrain t)
	{
		float[,,] splatmapData = new float[size,size,t.terrainData.alphamapLayers];

		for (int z=0; z<size;z++)
		{
			for (int x=0; x<size; x++)
			{
				float height = t.terrainData.GetHeight(z,x);
				float nx = (float)x * 1.0f / (float)(size - 1);
				float nz = (float)z * 1.0f / (float)(size - 1);

				int indexToApply = getLandType(height);
				for (int i=0; i<t.terrainData.alphamapLayers; i++)
				{
					splatmapData[x,z,i] = 0;
				}
				splatmapData[x,z,indexToApply] = 1;
			}
		}
		return splatmapData;
	}

	int getLandType(float elevation)
	{
		//make el a fraction of the highest height
		float el = elevation;
		if (el < seaLevel) {return waterIndex;}
		else if(el > seaLevel && el < grassLevel) {return sandIndex;}
		else if (el > grassLevel && el < rockLevel) {return grassIndex;}
		else if (el > rockLevel && el < snowLevel) {return rockIndex;}
		else if (el > snowLevel) {return snowIndex;}
		else return nullIndex;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
