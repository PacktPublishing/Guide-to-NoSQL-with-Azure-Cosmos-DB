SELECT v.id AS videoGameId, 
    v.name AS videoGameName, 
    v.levels AS videoGameLevels
FROM Videogames v
WHERE ARRAY_CONTAINS(v.levels,
    {
        "title": "Jungle Arena",
        "towers": 2
    })
