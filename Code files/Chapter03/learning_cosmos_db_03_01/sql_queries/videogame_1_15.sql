SELECT *
FROM h IN Videogames.highestScores
WHERE h.player.experienceLevel > 120
