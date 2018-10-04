SELECT v.id AS videoGameId, 
    v.name AS videoGameName, 
    v.levels[0] AS firstLevel
FROM videogames v
WHERE IS_ARRAY(v.levels)
