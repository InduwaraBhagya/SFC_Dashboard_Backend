$(document).ready(function() {
    // All initialization code consolidated here
    
    // Make the next task card clickable on mobile
    const nextTaskCard = document.getElementById('next-task-card');
    if (nextTaskCard) {
        nextTaskCard.addEventListener('click', function(e) {
            // Don't trigger if clicking on the "View All" or "Start Working" buttons
            if (!e.target.closest('a')) {
                const startButton = this.querySelector('.btn-primary');
                if (startButton && startButton.href) {
                    window.location.href = startButton.href;
                }
            }
        });
    }

    // Initialize message inbox
    initializeMessageInbox();
    
    // Refresh inbox button
    if (document.getElementById('refreshInbox')) {
        document.getElementById('refreshInbox').addEventListener('click', function() {
            window.location.reload();
        });
    }
    
    // Setup search functionality
    var select = document.getElementById('searchType');
    var input = document.getElementById('indexSearchInput');
    var label = document.getElementById('indexSearchLabel');

    if (select && input && label) {
        function updateInputNameAndPlaceholder() {
            input.name = select.value;
            switch (select.value) {
                case "peNumber":
                    input.placeholder = "Enter PE Number";
                    label.textContent = "PE Number:";
                    break;
                case "customer":
                    input.placeholder = "Enter Customer Name";
                    label.textContent = "Customer:";
                    break;
                case "jobReference":
                    input.placeholder = "Enter Job Reference";
                    label.textContent = "Job Reference:";
                    break;
                case "soNumber":
                    input.placeholder = "Enter SO Number";
                    label.textContent = "SO Number:";
                    break;
            }
        }
        select.addEventListener('change', updateInputNameAndPlaceholder);
        updateInputNameAndPlaceholder();
    }

    // Handle urgent request buttons
    $(document).on('click', '#acceptUrgentRequestBtn', function() {
        const requestType = $('#urgentRequestType').val();
        const requestId = $('#urgentRequestId').val();
        let priorityText = $('#urgentRequestDetails .row:nth-child(3) .col-sm-8').text() || '';
        let urgentReason = 'OpeningCeremony';
        if (priorityText.includes('Priority 2') || priorityText.toLowerCase().includes('critical customer')) {
            urgentReason = 'CriticalCustomer';
        } else if (priorityText.includes('Priority 0') || priorityText.toLowerCase().includes('network outage')) {
            urgentReason = 'NetworkOutage';
        } else if (priorityText.includes('Priority 1') || priorityText.toLowerCase().includes('opening ceremony')) {
            urgentReason = 'OpeningCeremony';
        }
        const form = $('#urgentRequestForm');
        // Remove any previous urgentReason fields
        form.find('input[name="urgentReason"]').remove();
        $('<input>').attr({type: 'hidden', name: 'urgentReason', value: urgentReason}).appendTo(form);
        if (requestType === 'pe') {
            form.attr('action', '/PlannedEvents/ProcessUrgentRequest');
        } else {
            form.attr('action', '/PETasks/ProcessUrgentRequest');
        }
        form.submit();
    });

    $(document).on('click', '#rejectUrgentRequestBtn', function() {
        const requestType = $('#urgentRequestType').val();
        const requestId = $('#urgentRequestId').val();
        const form = $('#urgentRequestForm');
        // Remove any previous urgentReason fields
        form.find('input[name="urgentReason"]').remove();
        $('<input>').attr({type: 'hidden', name: 'urgentReason', value: 'Reject'}).appendTo(form);
        if (requestType === 'pe') {
            form.attr('action', '/PlannedEvents/ProcessUrgentRequest');
        } else {
            form.attr('action', '/PETasks/ProcessUrgentRequest');
        }
        form.submit();
    });
    
    // Issue resolution handlers
    $('#replyIssueBtn').on('click', function() {
        const issueId = $(this).data('issue-id');
        const peId = $(this).data('pe-id');
        const senderId = $(this).data('sender-id');
        const originalIssueId = $(this).data('original-issue-id') || issueId;
        
        // Set values for the reply form
        $('#replyIssueId').val(originalIssueId);
        $('#replyPlannedEventId').val(peId);
        $('#replyReceiverId').val(senderId);
        
        // Hide the details modal and show the reply modal
        $('#issueDetailsModal').modal('hide');
        $('#issueReplyModal').modal('show');
    });
    
    $('#resolveIssueBtn').on('click', function() {
        const issueId = $(this).data('issue-id');
        const peId = $(this).data('pe-id');
        
        // Set values for the resolve form
        $('#resolveIssueId').val(issueId);
        $('#resolvePlannedEventId').val(peId);
        
        // Hide the details modal and show the resolve modal
        $('#issueDetailsModal').modal('hide');
        $('#issueResolveModal').modal('show');
    });
    
    $('#confirmResolutionBtn').on('click', function() {
        const form = $('#confirmResolutionForm');
        
        // First make sure the input isn't duplicated if clicked multiple times
        form.find('input[name="isConfirmed"]').remove();
        
        // Add the isConfirmed field
        $('<input>').attr({
            type: 'hidden',
            name: 'isConfirmed',
            value: 'true'
        }).appendTo(form);
        
        // Actually submit the form
        form.submit();
        
        // Log the submission for debugging
        console.log('Submitting confirmation form with resolution ID:', $('#resolutionId').val());
    });
    
    $('#rejectResolutionBtn').on('click', function() {
        const form = $('#confirmResolutionForm');
        $('<input>').attr({
            type: 'hidden',
            name: 'isConfirmed',
            value: 'false'
        }).appendTo(form);
        form.submit();
    });
});

// Functions defined outside document ready to avoid scoping issues
function initializeMessageInbox() {
    const messageItems = document.querySelectorAll('.message-item');
    const filterButtons = document.querySelectorAll('.inbox-filters button');
    const inboxSearch = document.querySelector('.inbox-search');
    
    // Ensure all messages are visible on first load
    messageItems.forEach(item => {
        if (item.classList.contains('unread')) {
            item.style.display = 'flex';
        } else {
            item.style.display = 'none';
        }
    });

    // Message filtering
    filterButtons.forEach(button => {
        button.addEventListener('click', function() {
            // Remove active class from all buttons
            filterButtons.forEach(btn => btn.classList.remove('active'));
            // Add active class to clicked button
            this.classList.add('active');

            const filter = this.getAttribute('data-filter');

            // Show/hide messages based on filter
            messageItems.forEach(item => {
                const isUnread = item.classList.contains('unread');
                
                switch(filter) {
                    case 'unread':
                        // Show only unread messages
                        item.style.display = isUnread ? 'flex' : 'none';
                        break;
                    case 'read':
                        // Show only read messages
                        item.style.display = !isUnread ? 'flex' : 'none';
                        break;
                    case 'all':
                        // Show all messages
                        item.style.display = 'flex';
                        break;
                }
            });
        });
    });
    
    // Handle search functionality
    if (inboxSearch) {
        inboxSearch.addEventListener('input', function() {
            const searchTerm = this.value.toLowerCase();
            
            messageItems.forEach(item => {
                const content = item.textContent.toLowerCase();
                if (content.includes(searchTerm)) {
                    item.style.display = 'flex';
                } else {
                    item.style.display = 'none';
                }
            });
        });
    }

    // Handle message clicks for urgent requests
    document.querySelectorAll('.urgent-request').forEach(item => {
        item.addEventListener('click', function() {
            // Extract the ID from data-message-id attribute
            const messageId = this.getAttribute('data-message-id');
            const peId = messageId.replace('urgent-pe-', '');
            
            // Show PE urgent confirmation modal with details
            showUrgentRequestModal('pe', peId);
        });
    });

    // Handle task urgent request clicks
    document.querySelectorAll('.task-urgent-request').forEach(item => {
        item.addEventListener('click', function() {
            // Extract the ID from data-message-id attribute
            const messageId = this.getAttribute('data-message-id');
            const taskId = messageId.replace('urgent-task-', '');
            
            // Show task urgent confirmation modal
            showUrgentRequestModal('task', taskId);
        });
    });
}

function showUrgentRequestModal(type, id) {
    // Set the form values
    document.getElementById('urgentRequestType').value = type;
    document.getElementById('urgentRequestId').value = id;
    
    // Fetch record details based on type (PE or Task)
    let url = '';
    if (type === 'pe') {
        url = '/PlannedEvents/GetUrgentRequestDetails/' + id;
    } else {
        url = '/PETasks/GetUrgentRequestDetails/' + id;
    }
    
    // Show loading indicator
    document.getElementById('urgentRequestDetails').innerHTML = '<div class="text-center"><div class="spinner-border text-primary" role="status"></div><p class="mt-2">Loading details...</p></div>';
    
    // Show the modal while fetching details
    const modal = new bootstrap.Modal(document.getElementById('urgentRequestModal'));
    modal.show();
    
    // Fetch details from server
    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Update the modal with details
            let detailsHtml = `
                <div class="alert alert-info mb-3">
                    <div class="row">
                        <div class="col-sm-4 fw-bold">PE Number:</div>
                        <div class="col-sm-8">${data.peNumber}</div>
                    </div>
                    <div class="row mt-2">
                        <div class="col-sm-4 fw-bold">Customer:</div>
                        <div class="col-sm-8">${data.customer || 'Not specified'}</div>
                    </div>`;
                    
            if (type === 'task') {
                detailsHtml += `
                    <div class="row mt-2">
                        <div class="col-sm-4 fw-bold">Task:</div>
                        <div class="col-sm-8">${data.taskName}</div>
                    </div>`;
            }
            
            detailsHtml += `
                <div class="row mt-2">
                    <div class="col-sm-4 fw-bold">Priority:</div>
                    <div class="col-sm-8">${data.priority || 'Not specified'}</div>
                </div>
            </div>`;
            
            // Show request reason if available
            if (data.urgentRequestReason) {
                detailsHtml += `
                <div class="alert alert-warning">
                    <h6 class="fw-bold">Request Reason:</h6>
                    <p>${data.urgentRequestReason}</p>
                </div>`;
            }
            
            document.getElementById('urgentRequestDetails').innerHTML = detailsHtml;
        })
        .catch(error => {
            console.error('Error fetching urgent request details:', error);
            document.getElementById('urgentRequestDetails').innerHTML = `
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-circle me-2"></i>
                    Failed to load request details. Please try again.
                </div>`;
        });
}

function showIssueDetails(element) {
    // Extract issue data from the element's data attributes
    const issueId = $(element).data('issue-id');
    const peId = $(element).data('pe-id');
    const sender = $(element).data('sender');
    const senderId = $(element).data('sender-id');
    const issueText = $(element).data('issue-text');
    const attachment = $(element).data('attachment');
    const date = $(element).data('date');
    const isRead = $(element).data('is-read') === 'true';
    const isResolutionRequest = $(element).data('is-resolution-request') === 'true';
    const originalIssueId = $(element).data('original-issue-id');
    
    // Build the issue details content
    let detailsHtml = `
        <div class="message-detail-header">
            <h5>${isResolutionRequest ? '<span class="badge bg-success">Resolution Request</span> ' : ''}${issueText}</h5>
            <div class="message-info">
                <span class="fw-bold">From:</span> ${sender} · 
                <span class="fw-bold">Date:</span> ${date}
            </div>
        </div>
        <div class="message-detail-body my-3">
            <p>${issueText}</p>
            ${attachment ? `<div class="attachment-section mt-3">
                <h6 class="fw-bold">Attachment:</h6>
                <a href="${attachment}" target="_blank" class="btn btn-sm btn-outline-primary">
                    <i class="fas fa-paperclip me-2"></i>View Attachment
                </a>
            </div>` : ''}
        </div>`;
    
    // Update the modal content
    $('#issueDetailsContent').html(detailsHtml);
    $('#viewPEDetailsBtn').attr('href', `/PlannedEvents/Details/${peId}`);
    
    // Set up the reply button data
    $('#replyIssueBtn').data('issue-id', issueId);
    $('#replyIssueBtn').data('pe-id', peId);
    $('#replyIssueBtn').data('sender-id', senderId);
    $('#replyIssueBtn').data('original-issue-id', originalIssueId);
    
    // Set up the resolve button data
    $('#resolveIssueBtn').data('issue-id', issueId);
    $('#resolveIssueBtn').data('pe-id', peId);
    
    // Handle resolution request specific UI
    if (isResolutionRequest) {
        $('#replyIssueBtn').addClass('d-none');
        $('#resolveIssueBtn').addClass('d-none');
        
        // Check if there's a resolution record for this issue
        $.get(`/Issues/GetResolution/${originalIssueId || issueId}`, function(resolution) {
            if (resolution && resolution.id) {
                // Show resolution confirmation section
                $('#resolutionConfirmationSection').removeClass('d-none');
                $('#resolutionId').val(resolution.id);
            }
        });
    } else {
        $('#replyIssueBtn').removeClass('d-none');
        $('#resolveIssueBtn').removeClass('d-none');
        $('#resolutionConfirmationSection').addClass('d-none');
    }
    
    // Fetch PE record details
    fetchPEDetails(peId);
    
    // Mark as read if not already read
    if (!isRead) {
        markIssueAsRead(issueId);
        $(element).removeClass('unread');
        $(element).data('is-read', 'true');
        
        // Update unread count in the header
        updateUnreadCount();
    }
    
    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('issueDetailsModal'));
    modal.show();
}

function showResolutionConfirm(element) {
    const resolutionId = $(element).data('resolution-id');
    const resolutionDetails = $(element).data('resolution-details');
    const peId = $(element).data('pe-id');
    const issueId = $(element).data('issue-id');
    
    console.log('Resolution data:', {
        resolutionId,
        resolutionDetails,
        peId,
        issueId
    });
    
    // Always clear previous values and content
    $('#directResolutionId').val('');
    $('#resolutionDetailsText').html('<div class="spinner-border spinner-border-sm text-primary" role="status"></div><span class="ms-2">Loading resolution details...</span>');
    
    // Check if we have the resolution ID from the data attribute
    if (resolutionId && resolutionId !== '0') {
        $('#directResolutionId').val(resolutionId);
        
        // If we also have the details from the attribute, use them directly
        if (resolutionDetails && String(resolutionDetails).trim() !== '') {
            $('#resolutionDetailsText').text(String(resolutionDetails));
        } else {
            // Otherwise fetch the details for this resolution
            fetchResolutionDetails(resolutionId);
        }
    } else {
        // If no resolution ID from data attribute, query by issue ID
        fetchResolutionByIssueId(issueId);
    }
    
    // Set up other modal elements
    $('#viewPEDetailsLink').attr('href', `/PlannedEvents/Details/${peId}`);
    
    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('resolutionConfirmModal'));
    modal.show();
    
    // Mark as read
    if (issueId) {
        markIssueAsRead(issueId);
        $(element).removeClass('unread');
        $(element).data('is-read', 'true');
        updateUnreadCount();
    }
}

function fetchResolutionDetails(resolutionId) {
    fetch(`/Issues/GetResolutionById/${resolutionId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data && data.details) {
                $('#resolutionDetailsText').text(data.details);
            } else {
                $('#resolutionDetailsText').html('<em>No resolution details available</em>');
            }
        })
        .catch(err => {
            console.error('Error fetching resolution details:', err);
            $('#resolutionDetailsText').html('<em class="text-danger">Could not load resolution details. Please try again later.</em>');
        });
}

function fetchResolutionByIssueId(issueId) {
    fetch(`/Issues/GetResolution/${issueId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data && data.id) {
                $('#directResolutionId').val(data.id);
                $('#resolutionDetailsText').text(data.details || 'No details provided');
            } else {
                $('#resolutionDetailsText').html('<em>No resolution found for this issue</em>');
                // Hide the buttons if no resolution is found
                $('#confirmResolutionForm button').hide();
            }
        })
        .catch(err => {
            console.error('Error fetching resolution:', err);
            $('#resolutionDetailsText').html('<em class="text-danger">Could not load resolution details. Please try again later.</em>');
        });
}

function submitResolutionForm(isConfirmed) {
    // Set the hidden field value based on the button clicked
    $('#directIsConfirmed').val(isConfirmed);
    
    // Get the resolution ID and validate it
    const resolutionId = $('#directResolutionId').val();
    console.log('Submitting form with Resolution ID:', resolutionId);
    console.log('Is Confirmed:', isConfirmed);
    
    if (!resolutionId || resolutionId === '0') {
        alert('Error: No resolution ID found. Please refresh the page and try again.');
        return;
    }
    
    // Disable buttons to prevent double submission
    const buttons = $('#confirmResolutionForm button');
    buttons.prop('disabled', true);
    
    // Add a spinner to the button that was clicked
    const clickedButton = isConfirmed ? 
        $('#confirmResolutionForm button:contains("Yes")') : 
        $('#confirmResolutionForm button:contains("No")');
    
    const originalButtonHtml = clickedButton.html();
    clickedButton.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...');
    
    // Submit the form
    try {
        document.getElementById('confirmResolutionForm').submit();
    } catch (err) {
        console.error('Error submitting form:', err);
        alert('Error submitting form. Please try again.');
        
        // Re-enable buttons on error
        buttons.prop('disabled', false);
        clickedButton.html(originalButtonHtml);
    }
}

function fetchPEDetails(peId) {
    // Fetch PE details
    fetch(`/PlannedEvents/GetBasicDetails/${peId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Update the PE details section with basic info
            let peDetailsHtml = `
                <div class="row">
                    <div class="col-md-6">
                        <div class="mb-2"><strong>PE Number:</strong> ${data.peNumber}</div>
                        <div class="mb-2"><strong>Customer:</strong> ${data.customer || 'N/A'}</div>
                        <div class="mb-2"><strong>Status:</strong> <span class="badge ${data.peStatus === 'Hold' ? 'bg-warning' : 'bg-primary'}">${data.peStatus}</span></div>
                    </div>
                    <div class="col-md-6">
                        <div class="mb-2"><strong>Service Type:</strong> ${data.serviceType || 'N/A'}</div>
                        <div class="mb-2"><strong>Task:</strong> ${data.taskName}</div>
                        <div class="mb-2"><strong>Workgroup:</strong> ${data.taskWg}</div>
                    </div>
                </div>`;
            
            $('#relatedPEDetails').html(peDetailsHtml);
        })
        .catch(error => {
            console.error('Error fetching PE details:', error);
            $('#relatedPEDetails').html(`
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-circle me-2"></i>
                    Failed to load PE details. Please try again.
                </div>`);
        });
}

function updateUnreadCount() {
    const unreadBadge = document.querySelector('.inbox-header .badge');
    const currentCount = parseInt(unreadBadge?.textContent) || 0;
    
    if (currentCount > 1) {
        const newCount = currentCount - 1;
        unreadBadge.textContent = `${newCount} Unread`;
    } else if (currentCount === 1) {
        unreadBadge.remove();
    }
}

function markIssueAsRead(issueId) {
    // Call API to mark issue as read
    fetch(`/Issues/MarkAsRead/${issueId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            // Get the anti-forgery token from any form on the page
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        }
    }).then(response => {
        if (!response.ok) {
            console.error('Error marking issue as read');
        }
    });
    updateUnreadCount();
}

function getCurrentUserId() {
    return document.getElementById('currentUserId') ? document.getElementById('currentUserId').value : null;
}