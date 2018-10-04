SELECT v.name AS videoGameName, 
    h.player.nickName AS playerNickName, 
    h.score AS highScore
FROM Videogames v
JOIN h IN v.highestScores
