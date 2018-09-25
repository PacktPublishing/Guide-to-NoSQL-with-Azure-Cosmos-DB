SELECT v.id AS videoGameId, 
    v.name AS videoGameName, 
    v.levels[0] AS firstLevel
FROM Videogames v
WHERE IS_ARRAY(v.levels) 
AND ARRAY_LENGTH(v.levels) >= 1
