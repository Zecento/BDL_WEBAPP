function isLogged() {
    return localStorage.getItem("userID") != null;
}

function logout() {
    localStorage.removeItem("userID");
    localStorage.removeItem("userEmail");
}

function retrieveUserEmail() {
    return localStorage.getItem("userEmail");
}

(() => {
    const logoutBtn = document.getElementById("log-out-btn");
    const favsBtn = document.getElementById("favs-btn");
    if(!isLogged()) {
        if(window.location.pathname !== "/Home/Login") window.location.href = "/Home/Login";
        logoutBtn.style.display = "none";
        favsBtn.style.display = "none";
    } else {
        favsBtn.style.display = "block";
        favsBtn.addEventListener("click", function(){
            window.open("/Home/Favorites?userId=" + retrieveUserId(), '_blank');
        });
        logoutBtn.style.display = "block";
        logoutBtn.addEventListener("click", function(){
            logout();
            window.location.reload();
        });
        logoutBtn.innerHTML = "Logout (" + retrieveUserEmail() + ")";
    }
})()