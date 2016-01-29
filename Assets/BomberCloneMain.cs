using UnityEngine;
using Gamelogic;
using Gamelogic.Grids;
using Gamelogic.Grids.Examples;
using System.Collections.Generic;
using System.Collections;


namespace BomberClone
{
	public class BomberCloneMain : GLMonoBehaviour
	{
		public GameObject playerPrefab;
		public GameObject enemyPrefab;
		public GameObject bombPrefab;
		public SpriteCell cellPrefab;
		public GameObject root;
		public GameObject blastPrefab;

		private const int GRID_WIDTH = 15;
		private const int GRID_HEIGHT = 10;
		private const float MOVE_AMOUNT = 7.50f;
		private const int MAX_BOMBS = 5;
		private const float BOMB_DELAY = 2.0f;
		private const float BLAST_DELAY = 0.25f;
		private const int BLAST_RADIUS = 1;

		// The grid data structure that contains all cell.
		private RectGrid<SpriteCell> grid;
		
		// The map (that converts between world and grid coordinates).
		private IMap3D<RectPoint> map;

		private GameObject player;
		private List<GameObject> enemies;
		private List<GameObject> bombs;
		
		public void Start()
		{
			BuildGrid();
			player = (GameObject)Instantiate (playerPrefab);
			player.transform.parent = root.transform;

			SpriteCell firstCell = grid.GetCell( new RectPoint(0,0) );
			Vector3 firstCellPosition = firstCell.transform.localPosition;

			player.transform.localPosition = firstCellPosition;
			player.transform.Translate (Vector3.forward * -50.0f);
			player.transform.localScale = Vector3.one * 100.0f;

			enemies = new List<GameObject> ();
			bombs = new List<GameObject> ();

			for (int i = 0; i < 3; i++) {
				GameObject newEnemy = (GameObject)Instantiate (enemyPrefab);
				int randomX = Random.Range(0, GRID_WIDTH - 1);
				int randomY = Random.Range(0, GRID_HEIGHT - 1);
				Vector3 randomCellPosition = grid.GetCell(new RectPoint(randomX, randomY)).transform.localPosition;
				newEnemy.transform.parent = root.transform;
				newEnemy.transform.localPosition = randomCellPosition;
				newEnemy.transform.Translate (Vector3.forward * -50.0f);
				newEnemy.transform.localScale = Vector3.one * 100.0f;
				newEnemy.transform.SetLocalRotationX(45.0f);
				newEnemy.transform.SetLocalRotationY(45.0f);
				enemies.Add (newEnemy);
			}

		}
		
		private void BuildGrid()
		{
			// Creates a grid in a rectangular shape.
			grid = RectGrid<SpriteCell>.Rectangle(GRID_WIDTH, GRID_HEIGHT);
			
			// Creates a map...
			map = new RectMap(cellPrefab.Dimensions) 
				.WithWindow(ExampleUtils.ScreenRect)
					.AlignMiddleCenter(grid)
					.To3DXY();
			
			
			foreach (RectPoint point in grid) //Iterates over all points (coordinates) contained in the grid
			{
				SpriteCell cell = Instantiate(cellPrefab); 
				
				Vector3 worldPoint = map[point];
				
				cell.transform.parent = root.transform; 
				cell.transform.localScale = Vector3.one;
				cell.transform.localPosition = worldPoint;
				
				//if ( point.X % 2 == 1 && point.Y % 2 == 0)
				if ( Random.Range(0,100) > 85)
				{
					cell.Color = Color.black;
				} else {
					cell.Color = Color.blue;
				}

				cell.name = point.ToString(); // Makes it easier to identify cells in the editor.
				grid[point] = cell; // Finally, put the cell in the grid.
			}
		}
		
		public void Update()
		{

			foreach( GameObject enemy in enemies) {
				enemy.transform.RotateAroundX( Time.deltaTime * 100.0f );
			}

			foreach (GameObject bomb in bombs) {
				bomb.transform.localScale = bomb.transform.localScale * ( 1f + Time.deltaTime / 7.5f);
			}

			RectPoint playerCoords = map[player.transform.position];
			//Debug.Log ( playerCoords );
			//List<RectPoint> validNeighbors = (List<RectPoint>)grid.GetAllNeighbors (playerCoords);

			if (Input.GetKey (KeyCode.LeftArrow)) {
				Vector3 newPosition = player.transform.position + Vector3.left * MOVE_AMOUNT;
				RectPoint newPlayerCoords = map[newPosition];
				if (grid.Contains( newPlayerCoords ) && grid[newPlayerCoords].Color != Color.black ) {
					player.transform.TranslateX( -MOVE_AMOUNT );
				}
			}
			if (Input.GetKey (KeyCode.RightArrow)) {
				Vector3 newPosition = player.transform.position + Vector3.right * MOVE_AMOUNT;
				RectPoint newPlayerCoords = map[newPosition];
				if (grid.Contains( newPlayerCoords ) && grid[newPlayerCoords].Color != Color.black) {
					player.transform.TranslateX( MOVE_AMOUNT );
				}
			}
			if (Input.GetKey (KeyCode.UpArrow)) {
				Vector3 newPosition = player.transform.position + Vector3.up * MOVE_AMOUNT;
				RectPoint newPlayerCoords = map[newPosition];
				if (grid.Contains( newPlayerCoords ) && grid[newPlayerCoords].Color != Color.black) {
					player.transform.TranslateY( MOVE_AMOUNT );
				}
			}
			if (Input.GetKey (KeyCode.DownArrow)) {
				Vector3 newPosition = player.transform.position + Vector3.down * MOVE_AMOUNT;
				RectPoint newPlayerCoords = map[newPosition];
				if (grid.Contains( newPlayerCoords ) && grid[newPlayerCoords].Color != Color.black) {
					player.transform.TranslateY( -MOVE_AMOUNT );
				}
			}
			if (Input.GetKeyDown (KeyCode.Space)) {
				Debug.Log(bombs.Count);
				if ( bombs.Count < MAX_BOMBS) {
					GameObject bomb = (GameObject)Instantiate(bombPrefab);
					bomb.transform.parent = root.transform;
					bomb.transform.localPosition = grid.GetCell(playerCoords).transform.localPosition;
					bomb.transform.Translate (Vector3.forward * -50.0f);
					bomb.transform.localScale = Vector3.one * 100.0f;
					bombs.Add (bomb);
					StartCoroutine(SetBomb(bomb));
				}
			}

			foreach (GameObject enemy in enemies) 
			{
				if (Random.Range(0,1000) >= 995)
				{
					PointList<RectPoint> feasiblePoints = grid.GetNeighbors(map[ enemy.transform.localPosition ]).ToPointList();
					feasiblePoints.RemoveAll( r =>  grid[r].Color == Color.black );
					RectPoint selectedPoint = feasiblePoints.RandomItem();
					enemy.transform.localPosition = grid[selectedPoint].transform.localPosition;

				}
			}


			RectPoint newPlayerCoord = map [ player.transform.localPosition ];
			foreach (GameObject enemy in enemies) 
			{
				RectPoint enemyCoord = map[ enemy.transform.localPosition ];
				if ( newPlayerCoord == enemyCoord ) {
					Destroy ( player.gameObject );
				}

			}
		}

		private IEnumerator SetBomb(GameObject bomb) 
		{
			yield return new WaitForSeconds(BOMB_DELAY);
			bombs.Remove (bomb);
			CreateBlast ( map[bomb.transform.localPosition] );
			Destroy ( bomb.gameObject );
		}

		private IEnumerator Blast(GameObject blast) 
		{
			yield return new WaitForSeconds(BLAST_DELAY);
			Destroy ( blast.gameObject );
		}

		private void CreateBlast( RectPoint blastOrigin )
		{
			foreach ( RectPoint point in grid.GetNeighborHood( blastOrigin, BLAST_RADIUS )) 
			{
				if ( grid[point].Color != Color.black )
				{
					GameObject blast = (GameObject)Instantiate ( blastPrefab );
					blast.transform.parent = grid[point].transform;
					blast.transform.localPosition = Vector3.zero;
					blast.transform.localScale = Vector3.one * 100.0f;

					if ( map[ player.transform.localPosition ] == point )
					{
						Destroy (player.gameObject);
					}
					List<GameObject> deadEnemies = new List<GameObject>();
					foreach( GameObject enemy in enemies )
					{
						if (map[ enemy.transform.localPosition ] == point)
						{
							deadEnemies.Add (enemy);
						}
					}

					foreach( GameObject enemy in deadEnemies)
					{
						enemies.Remove(enemy);
						Destroy(enemy.gameObject);
					}
					StartCoroutine( Blast(blast) );
				}
			}
			   
		}
	}


}