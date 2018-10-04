SELECT TOP 1 v.id AS videoGameId, 
    v.name AS videoGameName, 
    v.levels[0] AS firstLevel
FROM videogames v
WHERE IS_ARRAY(v.levels)
ORDER BY v.name
