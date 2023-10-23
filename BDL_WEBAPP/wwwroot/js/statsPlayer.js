function totalStats(playerId){
    resetModal();
    $('#modal-data-player').modal('show'); // open bootstrap modal
    fetch('/api/bdl/total_stats?id='+playerId)
    .then(function(response){
        return response.json();
    }).then(function(data) {
        $('#loading-stats-player').hide();
        $('#stats-table-modal').show();
        $('#points-scored-modal').text(data.totalPoints);
        $('#assists-modal').text(data.totalAssists);
        $('#rebounds-modal').text(data.totalRebounds);
        $('#modal-title-label').text(`All-Season stats for ${data.playerName}`);
    }).catch(function(error){
        console.log("[totalStats] : error =>", error);
        // ideally we would show a message to the user (in the DOM itself)
        // for now, since it's a demo app, just close the modal.
        closeModal();
    });
}

function resetModal() {
    $('#loading-stats-player').show();
    $('#stats-table-modal').hide();
    $('#points-scored-modal').text('');
    $('#assists-modal').text('');
    $('#rebounds-modal').text('');
    $('#modal-title-label').text('All-Season stats for...');
}

function setUpModal(){
    $('#close-modal').on('click', function(){closeModal()});
    $('#close-modal-header').on('click', function(){closeModal()});
}

function closeModal(){
    $('#modal-data-player').modal('hide');
}

(function(){
    setUpModal();
})();