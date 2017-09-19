﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // used for Sum of array

class TerrainTile
{
	public Terrain tile;
	public float creationTime;

	public TerrainTile(Terrain t, float ct)
	{
		tile = t;
		creationTime = ct;
	}
}

public class terrainGenerator : MonoBehaviour {

	public GameObject player;
	public Terrain defaultLand;

	public int size = 256;
	public int height = 200;

	public int oceanIndex = 0;
	public int beachIndex = 1;
	public int forestIndex = 9;
	public int plainsIndex = 7;
	public int cliffIndex = 12;
	public int mountainIndex = 13;

	[Range(0.0f, 1.0f)]
	public float seaLevel = 0.1f;
	[Range(0.0f, 1.0f)]
	public float beachLevel = 0.2f;
	[Range(0.0f, 1.0f)]
	public float landLevel = 0.3f;
	[Range(0.0f, 1.0f)]
	public float mountainLevel = 0.6f;
	[Range(0.0f, 1.0f)]
	public float snowLevel = 0.8f;
	[Range(0.0f, 90.0f)]
	public float steep = 20.0f;
	[Range(1, 1024)]
	public int treeDensity = 250;
	//scale of perlin noise plain to terrain, this makes it 1:1 by default, and altered by frequency variable
	private float scale = 0.00390625f;

	//slider variables
	[Range(0.0f, 10.0f)]
	public float biomeSize = 1f;
	[Range(0.0f, 7.0f)]
	public float frequency = 2.0f;
	[Range(0.0f, 1.0f)]
	public float o1 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o2 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o3 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o4 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o5 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o6 = 0.0f;
	[Range(0.0f, 10.0f)]
	public float power = 1.0f;

	[Range(0.0f, 1.0f)]
	public float m1 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m2 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m3 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m4 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m5 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m6 = 0.0f;

	public int cullRadius = 2;

	public float seedLand = 5000f;
	public float seedBiome = 3000f;
	Vector3 startPos;
	Hashtable chunkTable = new Hashtable();

	// Use this for initialization
	void Start ()
	{
		int cullRadiusX = cullRadius;
		int cullRadiusZ = cullRadius;

		//Set time and space origins
		this.gameObject.transform.position = Vector3.zero;
		startPos = Vector3.zero;
		float updateTime = Time.realtimeSinceStartup;

		//create starint planes
		for (int x =-cullRadiusX; x<cullRadiusX; x++)
		{
			for (int z=-cullRadiusZ; z<cullRadiusZ; z++)
			{
				//get position of the new landscape
				Vector3 pos = new Vector3((x*size+startPos.x),0,(z*size+startPos.z));

				Terrain t = generateTerrain(pos);

				//log the terrain in a table
				string chunkName = "chunk_"+((int)(pos.x/size))+"_"+((int)(pos.z/size));
				t.name = chunkName;
				TerrainTile tile = new TerrainTile(t, updateTime);
				chunkTable.Add(chunkName, tile);
			}
		}
		setNeighbors();
	}

	//Create terrain data, including heightmaps, splatmaps, biomes and features
	Terrain generateTerrain(Vector3 pos)
	{
		//make the terrain and its data
		Terrain t = (Terrain) Instantiate(defaultLand,pos,Quaternion.identity);
		t.terrainData.alphamapResolution = size+1;

		TerrainData newChunk = new TerrainData();
		newChunk.alphamapResolution = size;
		newChunk.heightmapResolution = size;
		newChunk.size = new Vector3(size,height,size);
		newChunk.SetHeights(0,0,generateHeights(t));
		t.terrainData = newChunk;

		//get the textures to be used
		t.terrainData.splatPrototypes = defaultLand.terrainData.splatPrototypes;

		//create ground textures
		float[,,] textures = createSplatmap(t);
		newChunk.SetAlphamaps(0,0,textures);

		//populate ground details
		newChunk.SetDetailResolution(size,16);
		newChunk.detailPrototypes = defaultLand.terrainData.detailPrototypes;
		newChunk.treePrototypes = defaultLand.terrainData.treePrototypes;
		t = populateGround(t);

		//final applying of things
		t.terrainData = newChunk;
		t.GetComponent<TerrainCollider>().terrainData = t.terrainData;
		t.Flush();
		return t;
	}

	//This is where all the customization takes place. Uses iterations of Perlin noise to generate a landscape
	//Makes a 2D array and adds float values to each cell that would map to a plane, making a 3D mountainous plane
	float[,] generateHeights(Terrain terr)
	{
		//make the 2D array. +1 on each size so there is no gap between this terrain plane and the ones to the north
		//and east
		int alteredSize = size+1;
		float[,] heights = new float[alteredSize,alteredSize];
		for (int z=0; z<alteredSize; z++)
		{
			for (int x=0; x<alteredSize; x++)
			{
				//get default location heights
				float xCoord = (seedLand+(terr.transform.position.x+x))*scale;
				float zCoord = (seedLand+(terr.transform.position.z+z))*scale;

				//put detail into the heightmap
				float e = (
					(o1 * Mathf.PerlinNoise(1*xCoord*frequency,1*zCoord*frequency)) + 
					(o2 * Mathf.PerlinNoise(2*xCoord*frequency,2*zCoord*frequency)) + 
					(o3 * Mathf.PerlinNoise(4*xCoord*frequency,4*zCoord*frequency)) + 
					(o4 * Mathf.PerlinNoise(8*xCoord*frequency,8*zCoord*frequency)) + 
					(o5 * Mathf.PerlinNoise(16*xCoord*frequency,16*zCoord*frequency)) + 
					(o6 * Mathf.PerlinNoise(32*xCoord*frequency,32*zCoord*frequency)));

				e /= (o1+o2+o3+o4+o5+o6);

				float finalHeight = Mathf.Pow(e,power);

				heights[z,x] = finalHeight;
			}
		}
		return heights;
	}

	float[,,] createSplatmap(Terrain t)
	{
		float[,,] splatmapData = new float[t.terrainData.alphamapWidth,
										   t.terrainData.alphamapHeight,
										   t.terrainData.alphamapLayers];

		for (int z=0; z<t.terrainData.alphamapHeight;z++)
		{
			for (int x=0; x<t.terrainData.alphamapWidth; x++)
			{
				float nx = (float)x * 1.0f / (float)(t.terrainData.alphamapWidth - 1);
				float nz = (float)z * 1.0f / (float)(t.terrainData.alphamapWidth - 1);
				float xCoord = (seedBiome+(t.transform.position.x+(x)))*scale;
				float zCoord = (seedBiome+(t.transform.position.z+(z)))*scale;

				//moisture map
				float m = (
					(m1 * Mathf.PerlinNoise(1*xCoord,1*zCoord)) + 
					(m2 * Mathf.PerlinNoise(2*xCoord,2*zCoord)) + 
					(m3 * Mathf.PerlinNoise(4*xCoord,4*zCoord)) + 
					(m4 * Mathf.PerlinNoise(8*xCoord,8*zCoord)) + 
					(m5 * Mathf.PerlinNoise(16*xCoord,16*zCoord)) + 
					(m6 * Mathf.PerlinNoise(32*xCoord,32*zCoord)));

				m /= (m1+m2+m3+m4+m5+m6);
				float terrainHeight = t.terrainData.GetHeight(z,x);
				float angle = t.terrainData.GetSteepness(nz, nx);
				int terrainType = getBiome(terrainHeight,m,angle);
				for (int j=0; j<t.terrainData.alphamapLayers; j++)
				{
					splatmapData[x,z,j] = 0;
				}
				splatmapData[x,z,terrainType] = 1;
			}
		}
		return splatmapData;
	}

	int getBiome(float elevation, float moisture, float angle)
	{
		//make el a fraction of the highest height
		float el = elevation/height;
		if (el < seaLevel)
		{
			return oceanIndex;
		}
		else if (el > seaLevel && el < landLevel)
		{
			return beachIndex;
		}
		else if (el > landLevel && el < mountainLevel)
		{
			if (angle > steep)
			{
				return plainsIndex;
			}
			else
			{
				return forestIndex;
			}
		}
		else if (el > mountainLevel && el < snowLevel)
		{
			if (angle > steep)
			{
				return cliffIndex;
			}
			else
			{
				return mountainIndex;
			}
		}
		else
		{
			if (angle > steep)
			{
				return 4; //tundra
			}
			else
			{
				return 5;
			}
		}
		//0=ocean
		//1=beach
		//2=scorched
		//3=bare
		//4=tundra
		//5=snow
		//6=temperate desert
		//7=shrubland
		//8=taiga
		//9=grassland
		//10=temperate deciduous forest
		//11=temperate rain forest
		//12=subtropical desert
		//13=tropical seasonal forest
		//14=tropical rainforest
	}

	Terrain populateGround(Terrain t)
	{
		
		int[,] newMap = new int[t.terrainData.detailWidth,t.terrainData.detailHeight];
		float[,,] maps = t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight);
		int unitsForest = 0;
		//apply grass
		for (int z = 0; z < t.terrainData.detailHeight; z++)
		{
			for (int x = 0; x < t.terrainData.detailHeight; x++)
			{

				float height = t.terrainData.GetHeight(z,x);
				if (maps[z,x,plainsIndex] != 0f)
				{
					newMap[z, x] = 1;
					unitsForest++;
				}
				else if (maps[z,x,forestIndex] != 0f)
				{
					newMap[z, x] = 5;
					unitsForest++;
				}
				else
				{
					newMap[z, x] = 0;
				}
			}
		}
		t.terrainData.SetDetailLayer(0, 0, 0, newMap);

		//apply trees
		TreeInstance[] trees = new TreeInstance[size/2];
		bool[,] placement = new bool[t.terrainData.alphamapWidth,t.terrainData.alphamapHeight]; //true=tree there
		float plainsTreeChance = 0.5f;
		float beachTreeChance = 0.01f;
		float mountainTreeChance = 0.01f;

		int numTrees = 0;
		float realDensity = ((float)unitsForest/((float)size*(float)size))*(float)treeDensity;
		print("Input Density: "+treeDensity+" realDensity: "+realDensity+" unitsForest: "+unitsForest+" size"+size);
		while (numTrees < realDensity)
		{
			int x = Random.Range(0,t.terrainData.alphamapWidth);
			int z = Random.Range(0,t.terrainData.alphamapHeight);

			if (!placement[x,z])
			{
				float chance = Random.value;
				bool canPlace = (maps[z,x,forestIndex] != 0f) ||
					(maps[z,x,plainsIndex] != 0f && chance < plainsTreeChance) ||
					(maps[z,x,beachIndex] != 0f && chance < beachTreeChance) ||
					(maps[z,x,mountainIndex] != 0f && chance < mountainTreeChance);
				
				if (canPlace)
				{
						TreeInstance tree = createTree(x,z,t);
					placement[x,z] = true;
					numTrees++;
					t.AddTreeInstance(tree);
				}
			}
		}
			
		return t;
	}

	TreeInstance createTree(int x, int z, Terrain t)
	{
		//init setup of tree
		TreeInstance tree = new TreeInstance();
		tree.color = Color.white;
		tree.heightScale = 1;
		tree.widthScale = 1;
		tree.lightmapColor = Color.white;

		tree.prototypeIndex = Random.Range(0,0);
		float nx = ((float)x/t.terrainData.alphamapWidth);
		float nz = ((float)z/t.terrainData.alphamapHeight);
		tree.position = new Vector3 (nx, t.terrainData.GetHeight(z,x), nz);

		return tree;
	}
	// Update is called once per frame

	//Goes through all terrain planes in the scene and 'joins' them so the LOD will be consistant
	void setNeighbors()
	{
		//get all terrain planes in the scene
		Terrain[] allTerrain = FindObjectsOfType(typeof(Terrain)) as Terrain[];

		//for each one...
		foreach (Terrain terrain in allTerrain)
		{
			//get the position of the plane
			int thisX = (int)(terrain.transform.position.x/size);
			int thisZ = (int)(terrain.transform.position.z/size);

			//establish each possible neighbor
			Terrain leftLink = null;
			Terrain rightLink = null;
			Terrain upLink = null;
			Terrain downLink = null;

			//find each neighbor and set them to the appropriate variable
			if (GameObject.Find("chunk_"+(thisX-1)+"_"+(thisZ)))
			{
				//print("chunk_"+(thisX-1)+"_"+(thisZ)+" is left neighbor to "+thisX+" "+thisZ);
				leftLink = GameObject.Find("chunk_"+(thisX-1)+"_"+(thisZ)).GetComponent<Terrain>();
			}
			if (GameObject.Find("chunk_"+(thisX+1)+"_"+(thisZ)))
			{
				//print("chunk_"+(thisX+1)+"_"+(thisZ)+" is right neighbor to "+thisX+" "+thisZ);
				rightLink = GameObject.Find("chunk_"+(thisX+1)+"_"+(thisZ)).GetComponent<Terrain>();
			}
			if (GameObject.Find("chunk_"+(thisX)+"_"+(thisZ+1)))
			{
				//print("chunk_"+(thisX)+"_"+(thisZ+1)+" is top neighbor to "+thisX+" "+thisZ);
				upLink = GameObject.Find("chunk_"+(thisX)+"_"+(thisZ+1)).GetComponent<Terrain>();
			}
			if (GameObject.Find("chunk_"+(thisX)+"_"+(thisZ-1)))
			{
				//print("chunk_"+(thisX)+"_"+(thisZ-1)+" is bottom neighbor to "+thisX+" "+thisZ);
				downLink = GameObject.Find("chunk_"+(thisX)+"_"+(thisZ-1)).GetComponent<Terrain>();
			}

			//set the neighbors of this terrain plane. If one of the parameters is null, it represents no adjacent
			//terrain plane. This is only the case for planes on the edge, not near the player.
			terrain.SetNeighbors(leftLink,upLink,rightLink,downLink);
		}
	}
}
