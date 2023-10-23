function changeLoaderText(text) {
    document.querySelector("#loading-status-text").innerHTML = text;
}

function hideLoader(){
    document.querySelector("#loading-status").style.display = "none";
    document.querySelector("#loader-separator-1").style.display = "none";
    document.querySelector("#loader-separator-2").style.display = "none";
}
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
        const endpoint = '/api/favorites/delete?id=' + id;
        
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
        const endpoint = '/api/favorites/create';
        
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
            if(!likeButton) return;
            likeButton.innerHTML = "Saved! ❤️";
            likeButton.setAttribute("data-favoriteid", favorite.id);
        });
    })
}

function readAllUserFavorites(userId){
    return new Promise((resolve, reject) => {
        try{
            const endpoint = '/api/favorites?userId=' + userId;

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

const sleep = (milliseconds) => {
    return new Promise(resolve => setTimeout(resolve, milliseconds))
}

async function autoLoadNextPage(n_times, fetch_all=false) {
    let last_page = parseInt(document.querySelector("#totalPages").value)
    let end_page;
    let error_encountered = false;
    for (let i = 1; i <= n_times; i++) {
        let current_page = last_page - i; // 436, 435, 434, 433, 432, 431, 430, 429...
        if(i === n_times) end_page = current_page;
        if(fetch_all) {
            document.querySelector("#loading-status-text").innerHTML = `  Loading page ${current_page}...`;
        }
        if (current_page < 1) break;
        const response = await fetch(`/api/bdl/stats?page=${current_page}`);
        if (!response.ok) {
            console.log(`Page ${current_page} failed to load`);
            // save the current page in the hidden input
            document.querySelector("#totalPages").value = current_page+1;
            // wait six seconds before trying again
            await sleep(6520);
            error_encountered = true;
            break;
        } else {
            const data = await response.json();
            appendToTable(data);
        }
        
        if(i === n_times) {
            document.querySelector("#totalPages").value = current_page;
            changeLoaderText("  Sorting Table...")
            sortTable("stats-table", 1).then(() => {
                hideLoader();
                let userId = retrieveUserId();
                if (!userId) return;
                if(window.location.pathname !== "/Home/Favorites") {
                    restoreFromStorage(userId);
                    if(!fetch_all){
                        showWarningNotAllDataLoaded();
                    }
                }
            });
        }
    }
    if(error_encountered){
        let n_left_times = parseInt(document.querySelector("#totalPages").value) - 1;
        console.log("[error_encountered] => Will load all remaining data N TIMES " + n_left_times);
        await autoLoadNextPage(n_left_times, true);
    }
}

function showWarningNotAllDataLoaded() {
    let n_more_times = parseInt(document.querySelector("#totalPages").value) - 1;
    document.querySelector("#paragraph-warning-data").style.display = "block";
    document.querySelector("#date-last-fetched").innerHTML = document.querySelector("#last-date").value;
    document.querySelector("#load-all-stats").style.display = "block";
    document.querySelector("#load-all-stats").addEventListener("click", async () => {
        document.querySelector("#load-all-stats").style.display = "none";
        document.querySelector("#paragraph-warning-data").style.display = "none";
        document.querySelector("#loading-status").style.display = "block";
        document.querySelector("#loading-status-text").innerHTML = "  Loading all data...";
        document.querySelector("#loader-separator-1").style.display = "block";
        document.querySelector("#loader-separator-2").style.display = "block";
        await autoLoadNextPage(n_more_times, true);
    })
}

function appendToTable(data) {
    let table = document.querySelector("#stats-table");
    let tbody = table.querySelector("tbody");
    data.forEach(stat => {
        let row = document.createElement("tr");
        row.id = `table-row-${stat.id}`;
        row.innerHTML = `
            <td id="player-name-table-row-${stat.id}">${stat.playerName}</td>
            <td id="points-scored-table-row-${stat.id}">${stat.pointsScored}</td>
            <td id="assists-table-row-${stat.id}" >${stat.assists}</td>
            <td id="rebounds-table-row-${stat.id}">${stat.rebounds}</td>
            <td>
                <div class="like-button">
                    <button id="stat-season-${stat.id}"
                            data-playerName="${stat.playerName}"
                            onclick="totalStats(${stat.id})">TOT.STATS</button>
                </div>
            </td>
            <td>
                <div class="like-button">
                    <button id="heart-button-${stat.id}"
                            data-playerName="${stat.playerName}"
                            onclick="toggleLike(${stat.id})">Save to Favorites  🤍</button>
                </div>
            </td>
        `;
        // also create in this playersInfo map a new entry with the player name and the id
        // or update the existing one with the new points, assists and rebounds
        let playerInfo = playersInfoMap.get(stat.id);
        if(!playerInfo) {
            playersInfoMap.set(stat.id, {
                id: stat.id,
                playerName: stat.playerName,
                pointsScored: parseInt(stat.pointsScored),
                assists: parseInt(stat.assists),
                rebounds: parseInt(stat.rebounds),
                currentPage: parseInt(stat.currentPage),
            })
        } else {
            playersInfoMap.set(stat.id, {
                id: stat.id,
                playerName: stat.playerName,
                pointsScored: parseInt(playerInfo.pointsScored) + parseInt(stat.pointsScored),
                assists: parseInt(playerInfo.assists) + parseInt(stat.assists),
                rebounds: parseInt(playerInfo.rebounds) + parseInt(stat.rebounds),
                currentPage: parseInt(stat.currentPage),
            })
        }
        
        // append row only if id is not already in the table. (avoid duplicates)
        // if it is, sum the points scored, assists and rebounds
        let existingRow = document.querySelector(`#table-row-${stat.id}`);
        if(existingRow) {
            document.querySelector(`#points-scored-table-row-${stat.id}`).innerHTML = playersInfoMap.get(stat.id).pointsScored;
            document.querySelector(`#assists-table-row-${stat.id}`).innerHTML = playersInfoMap.get(stat.id).assists;
            document.querySelector(`#rebounds-table-row-${stat.id}`).innerHTML = playersInfoMap.get(stat.id).rebounds;
        } else {
            tbody.appendChild(row);
        }
    })
    // append gameDate to hidden input if not already present, else update it
    let input = document.querySelector("#last-date");
    if(!input) {
        input = document.createElement("input");
        input.type = "hidden"; input.id = "last-date";
        input.value = data[0].gameDate;
        document.body.appendChild(input);
    } else {
        input.value = data[0].gameDate;
    }
}

function sortTable(table_id, sortColumn){
    /*
        @param table_id: id of the table to sort
        @param sortColumn: index of the column to sort
        credits: https://stackoverflow.com/a/37814596/18290336
     */
    return new Promise((resolve, reject) => {
        var tableData = document.getElementById(table_id).getElementsByTagName('tbody').item(0);
        var rowData = tableData.getElementsByTagName('tr');
        for (var i = 0; i < rowData.length - 1; i++) {
            for (var j = 0; j < rowData.length - (i + 1); j++) {
                if (Number(rowData.item(j).getElementsByTagName('td').item(sortColumn).innerHTML.replace(/[^0-9\.]+/g, "")) < Number(rowData.item(j + 1).getElementsByTagName('td').item(sortColumn).innerHTML.replace(/[^0-9\.]+/g, ""))) {
                    tableData.insertBefore(rowData.item(j + 1), rowData.item(j));
                }
                if(i === rowData.length - 2 && j === rowData.length - (i + 2)) resolve();
            }
        }
    })
}

function changeCurrentSeason(){
    document.querySelector("#change-current-year").innerHTML = "List of Trendiest Players in " + (new Date().getFullYear()-1).toString() + " Season";
}

let playersInfoMap = new Map();

(async () => {
    let userId = retrieveUserId();
    if (!userId) return;
    if (window.location.pathname !== "/Home/Favorites") {
        restoreFromStorage(userId);
        changeCurrentSeason();
        let repeat_n_time = 9; // last 9 pages of the season
        await autoLoadNextPage(repeat_n_time);
    }
})();