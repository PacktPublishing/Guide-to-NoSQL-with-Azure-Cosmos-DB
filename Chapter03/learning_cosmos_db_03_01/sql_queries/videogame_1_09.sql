SELECT v.id AS videoGameId, 
    v.name AS videoGameName, 
    v.tags AS videoGameTags
FROM Videogames v
WHERE ARRAY_CONTAINS(v.tags, "monsters")
