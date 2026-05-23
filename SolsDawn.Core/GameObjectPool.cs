namespace SolsDawn.Core;

public static class GameObjectPool
{
    private static List<GameObject> _gameObjects = new();
    private static List<GameObject> _toAdd = new();
    private static List<GameObject> _toRemove = new();

    public static void Update()
    {
        _gameObjects.Sort();
        _toRemove.Sort();
        int i = 0; 
        int j = 0;
        while(j < _toRemove.Count)
        {
            if (_gameObjects[i] == _toRemove[j])
            {
                i++;
                j++;
            }
            else
            {
                _toAdd.Add(_gameObjects[i]);
                i++;
            }
        }

        while (i < _gameObjects.Count)
        {
            _toAdd.Add(_gameObjects[i]);
            i++;
        }

        (_gameObjects, _toAdd) = (_toAdd, _gameObjects);
        _toAdd.Clear();
        _toRemove.Clear();
        
        foreach (var go in _gameObjects)
            go.Update();
    }

    public static void LateUpdate()
    {
        foreach (var go in _gameObjects)
            go.LateUpdate();
    }

    public static void Draw()
    {
        foreach (var go in _gameObjects)
            go.Draw();
    }
    
    internal static void Add(GameObject gameObject) => _toAdd.Add(gameObject);
    internal static void Remove(GameObject gameObject) => _toRemove.Add(gameObject);
}