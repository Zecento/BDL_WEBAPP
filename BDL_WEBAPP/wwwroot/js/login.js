function saveLogin(userID, userEmail) {
    localStorage.setItem("userID", userID);
    localStorage.setItem("userEmail", userEmail);
}

function createUser(email){
    return new Promise((resolve, reject) => {
        try{
            const endpoint = '/api/users';

            const options = {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email })
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

function onblurListener(emailValue){
    // helper function for the "fancy" label animation
    let label = document.getElementById("input-label");
    if(emailValue.length <= 0) label.innerHTML = "Email";
    else label.innerHTML = "";
}

function validateEmail(emailValue){
    // Since it's a demo app, just check if it contains @.
    // In any case, it's always better to assume that the email is valid (by doing a regex check)
    // and send a confirmation email to the user to verify it.
    return emailValue.includes("@");
}

function login(btn,email){
    if(validateEmail(email.value)){
        btn.classList.remove("next-btn");
        btn.classList.add("disabled-next-btn");
        // it would be better to have a loading animation here
        createUser(email.value)
            .then(data => {
                saveLogin(data.id, data.email);
                window.location.href = "/";
            })
            .catch(error => {
                btn.classList.remove("disabled-next-btn");
                btn.classList.add("next-btn");
            });
    } else {
        // ideally we would show a message to the user (in the DOM itself)
        // for now, since it's a demo app, just show an alert
        alert("Invalid email")
    }
}

(() => {
    const email = document.getElementById("email");
    const btn = document.getElementById("log-in-btn");
    email.addEventListener("blur", function(){onblurListener(email.value)});
    btn.addEventListener("click", function(){
        login(btn,email);
    });
    email.addEventListener("keyup", function(event) {
        if (event.keyCode === 13) {
            event.preventDefault();
            login(btn,email);
        }
    })
})()