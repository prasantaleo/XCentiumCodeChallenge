$(document).ready(function () {
    $('#urlForm').on('submit', function (e) {
        e.preventDefault();
        const url = $('#url').val();
        if (!url) {
            alert("Please enter a valid URL.");
            return;
        }

        $.post('/Home/AnalyzeUrl', { url: url }, function (data) {
            $('#results').html(renderResults(data));

            $('#carouselExample').carousel({
                interval: 2000,
                ride: 'carousel',
                pause: "hover"
            });

        }).fail(function () {
            $('#results').html('<p class="text-danger">Failed to analyze the URL. Please try again.</p>');
        });

       
    });

    
    function renderResults(data) {
        

        // Display images if found       
        let html = '<h2>Image Gallery</h2>';
        if (data.imageUrls && data.imageUrls.length > 0) {
            html += '<div id="carouselExample" class="carousel slide" data-bs-ride="carousel"><div class="carousel-inner">';
            data.imageUrls.forEach((src, index) => {
                html += `<div class="carousel-item ${index === 0 ? 'active' : ''}"><img src="${src}" class="d-block w-100" /></div>`;
            });
            html += '</div><a class="carousel-control-prev" href="#carouselExample" role="button" data-bs-slide="prev"><span class="carousel-control-prev-icon" aria-hidden="true"></span><span class="sr-only">Previous</span></a><a class="carousel-control-next" href="#carouselExample" role="button" data-bs-slide="next"><span class="carousel-control-next-icon" aria-hidden="true"></span><span class="sr-only">Next</span></a></div>';
        } else {
            html += '<p>No images found.</p>';
        }

        // Display top words
        html += `<h2>Word Count: ${data.wordCount}</h2>`;
        if (Object.keys(data.topWords).length > 0) {
            html += '<h3>Top 10 Words</h3><table class="table"><thead><tr><th>Word</th><th>Count</th></tr></thead><tbody>';
            for (const [word, count] of Object.entries(data.topWords)) {
                html += `<tr><td>${word}</td><td>${count}</td></tr>`;
            }
            html += '</tbody></table>';
        } else {
            html += '<p>No significant words found.</p>';
        }

        return html;
    }
});