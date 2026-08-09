namespace SolsDawn.Core;

public static class GameObjectPool
{
    private static List<GameObject> _gameObjects = new();
    private static List<GameObject> _toAdd = new();
    private static List<GameObject> _toDispose = new();

    public static void Update()
    {
        _gameObjects.Sort();
        _toDispose.Sort();
        int i = 0; 
        int j = 0;
        while(j < _toDispose.Count)
        {
            if (_gameObjects[i] == _toDispose[j])
            {
                foreach(var component in _toDispose[j].Components)
                    component.Dispose();
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
        _toDispose.Clear();
        
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
    internal static void Destroy(GameObject gameObject) => _toDispose.Add(gameObject);
}