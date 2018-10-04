SELECT v.id
FROM Videogames v
WHERE IS_DEFINED(v.platforms)
