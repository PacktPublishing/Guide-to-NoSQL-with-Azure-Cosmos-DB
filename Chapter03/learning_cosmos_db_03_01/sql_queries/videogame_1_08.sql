SELECT v.id AS videoGameId, 
    v.name AS videoGameName, 
    ARRAY_SLICE(v.levels, 1, 2) AS selectedLevels
FROM Videogames v
WHERE IS_ARRAY(v.levels) 
AND (ARRAY_LENGTH(v.levels) >= 3)
