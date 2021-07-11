using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
	public Map[] maps;
	public int mapIndex;

	public Transform tilePrefab;
	public Transform obstaclePrefab;
	public Transform navmeshFloor;
	public Transform navmeshMaskPrefab;
	public Vector2 maxMapSize;

	[Range(0, 1)]
	public float outlinePercent;

	public float tileSize;//tỉ lệ ô hiển thị trên màn hình 
	List<Coord> allTileCoords;
	Queue<Coord> shuffledTileCoords;
	Queue<Coord> shuffledOpenTileCoords;
	Transform[,] tileMap;

	Map currentMap;

	void Awake()
	{
		FindObjectOfType<Spawner>().OnNewWave += OnNewWave;
	}

	void OnNewWave(int waveNumber)
	{
		mapIndex = waveNumber - 1;
		GenerateMap();
	}
	//Sinh map ngẫu nhiên , cách dễ nhất là đi vòng qua số lượng chướng ngại vật mà chúng ta muốn xuất hiện . Với mỗi vòng lặp  cần tìm tọa độ x,y để sih chướng ngại vật.
	//Vấn đề là có thể có 2 tọa độ ngẫu nhiên trùng nhau và tạo ra chướng ngại vật trùng nhau. Cách giải quyết là tạo 1 mảng con trỏ dến tọa độ toàn bản đồ và xáo trộn bên trong và bất kể mảng lớn đến đâu
	//chỉ cần lấy điểm đầu tiên làm điểm chướng ngại vật ngẫu nhiên
	//The fisher-yates
	public void GenerateMap()
	{
		currentMap = maps[mapIndex];
		tileMap = new Transform[currentMap.mapSize.x, currentMap.mapSize.y];
		System.Random prng = new System.Random(currentMap.seed);//lấy 1 số seed ngẫu nhiên từ currentMap để tạo độ cao trong khoảng max, min
		GetComponent<BoxCollider>().size = new Vector3(currentMap.mapSize.x * tileSize, .05f, currentMap.mapSize.y * tileSize);//TạO Box bao quanh map de k bi ra ngoai map

		// Generating coords
		allTileCoords = new List<Coord>();
		for (int x = 0; x < currentMap.mapSize.x; x++)
		{
			for (int y = 0; y < currentMap.mapSize.y; y++)
			{
				allTileCoords.Add(new Coord(x, y));
			}
		}
		shuffledTileCoords = new Queue<Coord>(Utility.ShuffleArray(allTileCoords.ToArray(), currentMap.seed));//Hàng đợi các tọa độ xáo trộn

		// Create map 
		string holderName = "Generated Map";
		if (transform.Find(holderName))
		{
			DestroyImmediate(transform.Find(holderName).gameObject);//nếu có map r thì xóa đi tạo map mới
		}

		Transform mapHolder = new GameObject(holderName).transform;
		mapHolder.parent = transform;

		// tạo đường kẻ
		for (int x = 0; x < currentMap.mapSize.x; x++)
		{
			for (int y = 0; y < currentMap.mapSize.y; y++)
			{
				Vector3 tilePosition = CoordToPosition(x, y);
				Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90)) as Transform;
				newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize;
				newTile.parent = mapHolder;
				tileMap[x, y] = newTile;
			}
		}

		// Sinh chướng ngại vật
		bool[,] obstacleMap = new bool[(int)currentMap.mapSize.x, (int)currentMap.mapSize.y]; //gán cho 1 bool ms bằng kích thước của bản đồ
		//Khai báo  bool của mảng hai chiều trc khi tạo chướng ngại vật
		//trc khi tạo chướng ngại vật mới vị trí chướng ngại vật sẽ đc cập nhật trên  obstacleMap và sau đó đặt map đó vào phương thức MapIsFullyAccessible

		int obstacleCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y * currentMap.obstaclePercent);// số vật thể = % tất cả các ô trong map
		int currentObstacleCount = 0;
		List<Coord> allOpenCoords = new List<Coord>(allTileCoords);//dsch toa do các tọa độ chướng ngại vật

		for (int i = 0; i < obstacleCount; i++)
		{
			Coord randomCoord = GetRandomCoord();
			obstacleMap[randomCoord.x, randomCoord.y] = true;//đc tạo thành công
			currentObstacleCount++;
			//nếu k tạo bất kì thứ gì ở trung tâm bản đồ và MapIsFullyAccessible() truy cập đc thì sẽ khởi tạo sinh chướng ngại vật	
			if (randomCoord != currentMap.mapCentre && MapIsFullyAccessible(obstacleMap, currentObstacleCount))
			{
				float obstacleHeight = Mathf.Lerp(currentMap.minObstacleHeight, currentMap.maxObstacleHeight, (float)prng.NextDouble());//độ cao chướng ngại vật
				Vector3 obstaclePosition = CoordToPosition(randomCoord.x, randomCoord.y);

				Transform newObstacle = Instantiate(obstaclePrefab, obstaclePosition + Vector3.up * obstacleHeight / 2, Quaternion.identity) as Transform;
				                                                                        // để chướng ngại vật k bị tụt xuống dưới map
				newObstacle.parent = mapHolder;
				newObstacle.localScale = new Vector3((1 - outlinePercent) * tileSize, obstacleHeight, (1 - outlinePercent) * tileSize);

				Renderer obstacleRenderer = newObstacle.GetComponent<Renderer>();
				Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial);
				//Xây dựng màu cho chướng ngại vật
				float colourPercent = randomCoord.y / (float)currentMap.mapSize.y;
				obstacleMaterial.color = Color.Lerp(currentMap.foregroundColour, currentMap.backgroundColour, colourPercent);
				obstacleRenderer.sharedMaterial = obstacleMaterial;

				allOpenCoords.Remove(randomCoord);//loại bỏ tọa độ ngãu nhiên đó của chướng ngại vật
			}
			else
			{
				obstacleMap[randomCoord.x, randomCoord.y] = false;//tạo thất bại
				currentObstacleCount--;
			}
		}

		shuffledOpenTileCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenCoords.ToArray(), currentMap.seed));//xáo trộn các ô còn lại 

		// Creating navmesh mask
		//che các khu vực navmesh k dùng tới
		Transform maskLeft = Instantiate(navmeshMaskPrefab, Vector3.left * (currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity) as Transform;
		maskLeft.parent = mapHolder;
		maskLeft.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, currentMap.mapSize.y) * tileSize;

		Transform maskRight = Instantiate(navmeshMaskPrefab, Vector3.right * (currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity) as Transform;
		maskRight.parent = mapHolder;
		maskRight.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, currentMap.mapSize.y) * tileSize;

		Transform maskTop = Instantiate(navmeshMaskPrefab, Vector3.forward * (currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity) as Transform;
		maskTop.parent = mapHolder;
		maskTop.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

		Transform maskBottom = Instantiate(navmeshMaskPrefab, Vector3.back * (currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity) as Transform;
		maskBottom.parent = mapHolder;
		maskBottom.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

		navmeshFloor.localScale = new Vector3(maxMapSize.x, maxMapSize.y) * tileSize;

	}
	//sử dụng thuật toán flood fill để ktra xem toàn bộ bản đồ có thể truy cập đc hay k ngay cả khi chướng ngại vật ms đc thêm vào
	//nếu đc thì thêm chương ngại vật vào map k thì quay lại tìm tọa độ mới
	// không có chướng ngại vật trung tâm vì vậy bắt đầu từ trung tâm của obstacleMap. Nó tìm kiếm các ô như 1 radar phát ra ngoài và đếm xem có bnhieu ô k có chướng ngại vật
	//sử dụng currentObstacleCount để đếm xem bnhieu ô k có chướng ngại vật 
	//Nếu giá trị  thu đc khác với số lượng ô k chứa chướng ngại vật phsir tồn tại điều đó có nghĩa là k tiếp cận đc tất cả các ô trong bản đồ, nó bị chặn bởi 1 chướng ngại vật trong TH đó map k thể truy cập đc và trả về false
	bool MapIsFullyAccessible(bool[,] obstacleMap, int currentObstacleCount)
	{
		bool[,] mapFlags = new bool[obstacleMap.GetLength(0), obstacleMap.GetLength(1)];
		Queue<Coord> queue = new Queue<Coord>();
		queue.Enqueue(currentMap.mapCentre);//ô trung tâm trống nên cho vào hàng đọi luôn
		mapFlags[currentMap.mapCentre.x, currentMap.mapCentre.y] = true;

		int accessibleTileCount = 1;// số lượng ô có thể truy cập

		while (queue.Count > 0)
		{
			Coord tile = queue.Dequeue();
		
			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					int neighbourX = tile.x + x;
					int neighbourY = tile.y + y;   
					if (x == 0 || y == 0)
					{
						if (neighbourX >= 0 && neighbourX < obstacleMap.GetLength(0) && neighbourY >= 0 && neighbourY < obstacleMap.GetLength(1))//đảm bảo tọa độ nằm bên trong map
						{    
							if (!mapFlags[neighbourX, neighbourY] && !obstacleMap[neighbourX, neighbourY])//Ktra xem ô MapFlags đã đc ktra chưa
							{  //Nếu ô này chưa đc chọn trc đó và có phải chướng ngại vậy k 
								mapFlags[neighbourX, neighbourY] = true;
								queue.Enqueue(new Coord(neighbourX, neighbourY));//tìm thấy các ô lân cận mà chưa ktra và thêm chúng vào hàng đợi và quay lại vòng lặp tiếp
//Xem xét tất cả các ô và loại trừ các ô chướng ngại vật trong map  nên nếu gặp 1 khối lập pương thì sẽ bị chặn k thể vươt qua 
								accessibleTileCount++;
							}
						}
					}
				}
			}
		}
		// có bnhieu ô có thể có sau khi sinh chướng ngại vật
		int targetAccessibleTileCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y - currentObstacleCount);
		return targetAccessibleTileCount == accessibleTileCount;
	}

	Vector3 CoordToPosition(int x, int y)
	{
		return new Vector3(-currentMap.mapSize.x / 2f + 0.5f + x, 0, -currentMap.mapSize.y / 2f + 0.5f + y) * tileSize;
	}

	public Transform GetTileFromPosition(Vector3 position)
	{
		int x = Mathf.RoundToInt(position.x / tileSize + (currentMap.mapSize.x - 1) / 2f);
		int y = Mathf.RoundToInt(position.z / tileSize + (currentMap.mapSize.y - 1) / 2f);
		x = Mathf.Clamp(x, 0, tileMap.GetLength(0) - 1);
		y = Mathf.Clamp(y, 0, tileMap.GetLength(1) - 1);
		return tileMap[x, y];
	}

	public Coord GetRandomCoord()
	{
		Coord randomCoord = shuffledTileCoords.Dequeue();
		shuffledTileCoords.Enqueue(randomCoord);
		return randomCoord;
	}

	//Lấy mục tiêu tiếp theo từ hàng đợi và trả về 1 tọa độ ngẫu nhiên
	public Transform GetRandomOpenTile()//nhận các ô ngãu nhiên từ các ô còn lại sau khi sinh chướng ngại vật
	{
		Coord randomCoord = shuffledOpenTileCoords.Dequeue();
		shuffledOpenTileCoords.Enqueue(randomCoord);
		return tileMap[randomCoord.x, randomCoord.y];
	}

	[System.Serializable]
	public struct Coord
	{
		public int x;
		public int y;

		public Coord(int _x, int _y)
		{
			x = _x;
			y = _y;
		}

		public static bool operator ==(Coord c1, Coord c2)
		{
			return c1.x == c2.x && c1.y == c2.y;
		}

		public static bool operator !=(Coord c1, Coord c2)
		{
			return !(c1 == c2);
		}

	}

	[System.Serializable]
	public class Map
	{

		public Coord mapSize;
		[Range(0, 1)]
		public float obstaclePercent;
		public int seed;
		public float minObstacleHeight;
		public float maxObstacleHeight;
		public Color foregroundColour;
		public Color backgroundColour;

		public Coord mapCentre
		{
			get
			{
				return new Coord(mapSize.x / 2, mapSize.y / 2);
			}
		}

	}
}
//Điểu hướng
//Unity k cho phép tự tạo điều hướng luois nab. Sau khi tạo 1 map lớn và có lưới điều hướng cho nó, nó sẽ che những phần nhỏ của bản đồ mà t k muốn (có nghĩ là tạo 1 map lưới điều hướng 
//lớn bao gồm toàn bộ bản đồ và loại trừ các phần nằm bên ngoài bản đồ thực sụ đc di chuyển xung quanh)
// Kiểm soát kích thước của bản đồ
//Ta cần che các chướng ngại vật trong navmesh để nvat, kẻ thù k thể vượt qua chúng, ta dùng nav mesh obstacle->Carve và nó tự động cắt navmesh.
//Ta cần làm tương tự cho các cạnh của map. Điểu này sẽ đảm bảo rằng kẻ thù k ra khỏi map và vượt qua chướng ngại vật hoặc đi lang thang ra khỏi bản đồ