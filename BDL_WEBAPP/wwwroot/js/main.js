function removeFromList(favoriteId) {
    // Note: Same as below (*NOTE (1))
    const row = document.getElementById(`table-row-${favoriteId}`);
    row.parentNode.removeChild(row);
    deleteFromStorage(favoriteId).then(r => {})
}
function toggleLike(playerId) {
    const likeButton = document.getElementById(`heart-button-${playerId}`);
    let isLiked = !likeButton.innerHTML.includes("Save to Favorites");
    if (isLiked) {
        let favoriteId = likeButton.getAttribute("data-favoriteid");
        if (favoriteId) {

            likeButton.innerHTML = "Save to Favorites 🤍";
            deleteFromStorage(favoriteId).then(r => {
                // reset the button
                likeButton.setAttribute("data-favoriteid", "");
            })
        }
        
    } else {
        // *NOTE (1) No need to wait for the save to finish to update the DOM.
        // We can do it in the background (a little bit like GMAIL does)
        // So the user can continue browsing and has instant feedback.
        // Even if the save fails, it's not a critical error.
        // Ideally, we would have a loading animation here
        let playerName = likeButton.getAttribute("data-playername");
        likeButton.innerHTML = "Saved! ❤️";
        saveToStorage(playerId, playerName).then(r => {
            // set the id of the favorite in the DOM so we can delete it later.
            likeButton.setAttribute("data-favoriteid", r.id);
        });
    }
}

function deleteFromStorage(id) {
    return new Promise((resolve, reject) => {
        const port = window.location.port;
        const endpoint = 'http://localhost:' + port + '/api/favorites/delete?id=' + id;
        
        const options = {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        };
        
        fetch(endpoint, options)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                resolve(data); // Resolve with the response data
            })
            .catch(error => {
                reject(error); // Reject with the error
            });
    });
}

function saveToStorage(playerId, playerName) {
    return new Promise((resolve, reject) => {
        let PlayerFirstName = playerName.split(" ")[0];
        let PlayerLastName = playerName.split(" ")[1];
        const userId = retrieveUserId();
        if (!userId) return;
        const port = window.location.port;
        const endpoint = 'http://localhost:' + port + '/api/favorites/create';
        
        const options = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId, playerId, PlayerFirstName, PlayerLastName })
        };
        
        fetch(endpoint, options)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                resolve(data); // Resolve with the response data
            })
            .catch(error => {
                reject(error); // Reject with the error
            });
    });
}
    

function restoreFromStorage(userId) {
    readAllUserFavorites(userId).then(favorites => {
        favorites.forEach(favorite => {
            const likeButton = document.getElementById(`heart-button-${favorite.playerId}`);
            likeButton.innerHTML = "Saved! ❤️";
            likeButton.setAttribute("data-favoriteid", favorite.id);
        });
    })
}

function readAllUserFavorites(userId){
    return new Promise((resolve, reject) => {
        try{
            const port = window.location.port;
            const endpoint = 'http://localhost:' + port + '/api/favorites?userId=' + userId;

            const options = {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            };

            fetch(endpoint, options)
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then(data => {
                    resolve(data); // Resolve with the response data
                })
                .catch(error => {
                    reject(error); // Reject with the error
                });

        } catch (error) {
            reject(error);
        }
    })
}

function retrieveUserId() {
    return localStorage.getItem("userID");
}

(() => {
    let userId = retrieveUserId();
    if (!userId) return;
    if(window.location.pathname !== "/Home/Favorites") restoreFromStorage(userId);
})();