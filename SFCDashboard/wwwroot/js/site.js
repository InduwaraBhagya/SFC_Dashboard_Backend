$(document).ready(function () {
    // Load escalations when page is loaded
    loadEscalations();
    
    // Refresh escalations every 2 minutes
    setInterval(loadEscalations, 2 * 60 * 1000);
    
    function loadEscalations() {
        $.ajax({
            url: '/Escalation/GetUserEscalations',
            method: 'GET',
            success: function (data) {
                updateEscalationUI(data);
            },
            error: function (error) {
                console.error("Error loading escalations:", error);
            }
        });
    }
    
    function updateEscalationUI(data) {
        var $bellIcon = $('.notification-bell');
        var $badge = $('.escalation-badge');
        var $count = $('.escalation-count');
        var $noEscalations = $('.no-escalations');
        var $escalationList = $('.escalation-list');
        
        // Clear previous escalations
        $escalationList.empty();
        
        // Update badge and count
        if (data.count > 0) {
            $badge.text(data.count).show();
            $count.text(data.count);
            $bellIcon.addClass('has-notifications');
            $noEscalations.hide();
            
            // Add escalations to dropdown
            $.each(data.escalations, function (i, escalation) {
                var escalationClass = '';
                var levelText = '';
                
                switch(escalation.level) {
                    case 1: 
                        escalationClass = 'escalation-engineer';
                        levelText = 'Engineer Level';
                        break;
                    case 2: 
                        escalationClass = 'escalation-dgm';
                        levelText = 'DGM Level';
                        break;
                    case 3: 
                        escalationClass = 'escalation-gm';
                        levelText = 'GM Level';
                        break;
                }
                
                // Format time as relative (e.g., "2 hours ago")
                var createdAt = new Date(escalation.createdAt);
                var timeAgo = timeSince(createdAt);
                
                var html = `
                    <a href="/Escalation/Details/${escalation.id}" class="dropdown-item escalation-item ${escalationClass}">
                        <div class="d-flex justify-content-between align-items-start">
                            <h6 class="escalation-title">Task #${escalation.taskId} OLA Violation</h6>
                            <span class="escalation-time">${timeAgo}</span>
                        </div>
                        <p class="escalation-message">
                            <span class="badge ${getBadgeClass(escalation.level)}">${levelText}</span>
                            ${truncateText(escalation.taskWorkGroup || 'Unknown workgroup', 25)}
                        </p>
                    </a>
                `;
                
                $escalationList.append(html);
            });
        } else {
            $badge.hide();
            $count.text('0');
            $bellIcon.removeClass('has-notifications');
            $noEscalations.show();
        }
    }
    
    function getBadgeClass(level) {
        switch(level) {
            case 1: return "bg-warning text-dark";
            case 2: return "bg-orange";
            case 3: return "bg-danger";
            default: return "bg-secondary";
        }
    }
    
    function truncateText(text, maxLength) {
        if (!text) return '';
        return text.length > maxLength ? text.substr(0, maxLength) + '...' : text;
    }
    
    function timeSince(date) {
        var seconds = Math.floor((new Date() - date) / 1000);
        
        var interval = Math.floor(seconds / 31536000);
        if (interval >= 1) return interval + " year" + (interval > 1 ? "s" : "") + " ago";
        
        interval = Math.floor(seconds / 2592000);
        if (interval >= 1) return interval + " month" + (interval > 1 ? "s" : "") + " ago";
        
        interval = Math.floor(seconds / 86400);
        if (interval >= 1) return interval + " day" + (interval > 1 ? "s" : "") + " ago";
        
        interval = Math.floor(seconds / 3600);
        if (interval >= 1) return interval + " hour" + (interval > 1 ? "s" : "") + " ago";
        
        interval = Math.floor(seconds / 60);
        if (interval >= 1) return interval + " minute" + (interval > 1 ? "s" : "") + " ago";
        
        return "just now";
    }
    
    // Only select filter buttons inside the notification dropdown
    document.querySelectorAll('.notification-dropdown .btn-group button[data-filter]').forEach(button => {
        button.addEventListener('click', function() {
            document.querySelectorAll('.notification-dropdown .btn-group button[data-filter]').forEach(btn => {
                btn.classList.remove('active');
            });
            this.classList.add('active');
            currentFilter = this.getAttribute('data-filter');
            loadCombinedNotifications();
        });
    });
    
    document.querySelectorAll('.notification-dropdown .btn-group button[data-type]').forEach(button => {
        button.addEventListener('click', function() {
            document.querySelectorAll('.notification-dropdown .btn-group button[data-type]').forEach(btn => {
                btn.classList.remove('active');
            });
            this.classList.add('active');
            currentType = this.getAttribute('data-type');
            loadCombinedNotifications();
        });
    });
});