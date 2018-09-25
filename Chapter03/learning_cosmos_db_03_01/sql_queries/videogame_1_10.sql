SELECT v.id AS videoGameId, 
    v.name AS videoGameName, 
    v.levels AS videoGameLevels
FROM Videogames v
WHERE ARRAY_CONTAINS(v.levels,
    {
        "title": "Rainbows after the storm",
        "maximumPlayers": 30,
        "minimumExperienceLevel": 60
    })
